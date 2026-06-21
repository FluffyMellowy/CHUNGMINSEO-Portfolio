/// The Hollow Rite - プレイヤーキャラクター（三人称・SFボスアクション）

#include "THRPlayerCharacter.h"
#include "Camera/CameraComponent.h"
#include "GameFramework/SpringArmComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Components/SkeletalMeshComponent.h"
#include "Engine/SkeletalMesh.h"
#include "Animation/AnimInstance.h"
#include "EnhancedInputComponent.h"
#include "EnhancedInputSubsystems.h"
#include "InputAction.h"
#include "InputActionValue.h"
#include "InputCoreTypes.h"
#include "InputMappingContext.h"
#include "Combat/THRHealthComponent.h"
#include "Combat/THRMeleeHitboxComponent.h"
#include "Boss/THRBossCharacter.h"
#include "THRProjectile.h"
#include "Engine/Engine.h"
#include "Engine/OverlapResult.h"
#include "Engine/World.h"
#include "DrawDebugHelpers.h"
#include "HAL/IConsoleManager.h"
#include "Kismet/GameplayStatics.h"
#include "TimerManager.h"
#include "UObject/ConstructorHelpers.h"
#include "TheHollowRite.h"

ATHRPlayerCharacter::ATHRPlayerCharacter()
{
	PrimaryActorTick.bCanEverTick = false;

	/* 三人称：キャラクターは移動方向を向き、コントローラのYawには追従しない */
	bUseControllerRotationPitch = false;
	bUseControllerRotationYaw = false;
	bUseControllerRotationRoll = false;

	UCharacterMovementComponent* CMC = GetCharacterMovement();
	CMC->bOrientRotationToMovement = true;
	CMC->RotationRate = FRotator(0.0f, 720.0f, 0.0f);
	CMC->MaxWalkSpeed = MoveSpeed;

	/* カメラブーム：キャラクターの背後に伸びる三人称アーム */
	CameraBoom = CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));
	CameraBoom->SetupAttachment(RootComponent);
	CameraBoom->TargetArmLength = 400.0f;
	CameraBoom->bUsePawnControlRotation = true; /* 視点入力でアームを回転 */
	CameraBoom->SocketOffset = FVector(0.0f, 0.0f, 60.0f);

	/* 追従カメラ：アーム先端に固定（アーム自身が回転を処理） */
	FollowCamera = CreateDefaultSubobject<UCameraComponent>(TEXT("FollowCamera"));
	FollowCamera->SetupAttachment(CameraBoom, USpringArmComponent::SocketName);
	FollowCamera->bUsePawnControlRotation = false;

	/* 体力コンポーネント */
	HealthComponent = CreateDefaultSubobject<UTHRHealthComponent>(TEXT("HealthComponent"));

	/* 近接攻撃ヒットボックス（モンタージュ攻撃へ移行する際にアニメ通知から駆動） */
	MeleeHitbox = CreateDefaultSubobject<UTHRMeleeHitboxComponent>(TEXT("MeleeHitbox"));

	/* ブラスター投射体のデフォルトクラス（BP不要で動作） */
	ProjectileClass = ATHRProjectile::StaticClass();

	/* スケルタルメッシュ（マネキン Manny）とアニメーションBPを割り当て */
	USkeletalMeshComponent* MeshComp = GetMesh();
	MeshComp->SetRelativeLocationAndRotation(FVector(0.0f, 0.0f, -88.0f), FRotator(0.0f, -90.0f, 0.0f));

	static ConstructorHelpers::FObjectFinder<USkeletalMesh> PlayerMeshFinder(
		TEXT("/Game/Characters/Mannequins/Meshes/SKM_Manny_Simple.SKM_Manny_Simple"));
	if (PlayerMeshFinder.Succeeded())
	{
		MeshComp->SetSkeletalMeshAsset(PlayerMeshFinder.Object);
	}

	static ConstructorHelpers::FClassFinder<UAnimInstance> PlayerAnimFinder(
		TEXT("/Game/Characters/Mannequins/Anims/Unarmed/ABP_Unarmed"));
	if (PlayerAnimFinder.Succeeded())
	{
		MeshComp->SetAnimInstanceClass(PlayerAnimFinder.Class);
	}

	/* 入力アセットのデフォルトロード */
	static ConstructorHelpers::FObjectFinder<UInputMappingContext> IMCFinder(
		TEXT("/Game/THR/Input/IMC_THR_Default.IMC_THR_Default"));
	static ConstructorHelpers::FObjectFinder<UInputAction> MoveFinder(
		TEXT("/Game/THR/Input/IA_THR_Move.IA_THR_Move"));
	static ConstructorHelpers::FObjectFinder<UInputAction> LookFinder(
		TEXT("/Game/THR/Input/IA_THR_Look.IA_THR_Look"));
	static ConstructorHelpers::FObjectFinder<UInputAction> DodgeFinder(
		TEXT("/Game/THR/Input/IA_THR_Dodge.IA_THR_Dodge"));
	static ConstructorHelpers::FObjectFinder<UInputAction> AttackFinder(
		TEXT("/Game/THR/Input/IA_THR_Attack.IA_THR_Attack"));

	/* 攻撃モンタージュのデフォルトロード */
	static ConstructorHelpers::FObjectFinder<UAnimMontage> AttackMontageFinder(
		TEXT("/Game/THR/Player/AM_Player_Punch.AM_Player_Punch"));
	if (AttackMontageFinder.Succeeded())
	{
		AttackMontage = AttackMontageFinder.Object;
	}

	if (IMCFinder.Succeeded())    DefaultMappingContext = IMCFinder.Object;
	if (MoveFinder.Succeeded())   MoveAction = MoveFinder.Object;
	if (LookFinder.Succeeded())   LookAction = LookFinder.Object;
	if (DodgeFinder.Succeeded())  DodgeAction = DodgeFinder.Object;
	if (AttackFinder.Succeeded()) AttackAction = AttackFinder.Object;
}

void ATHRPlayerCharacter::BeginPlay()
{
	Super::BeginPlay();

	/* デフォルト入力マッピングコンテキストを登録 */
	if (const APlayerController* PC = Cast<APlayerController>(GetController()))
	{
		if (UEnhancedInputLocalPlayerSubsystem* Subsystem =
			ULocalPlayer::GetSubsystem<UEnhancedInputLocalPlayerSubsystem>(PC->GetLocalPlayer()))
		{
			Subsystem->AddMappingContext(DefaultMappingContext, 0);
		}
	}

	/* 死亡デリゲートを購読 */
	if (HealthComponent)
	{
		HealthComponent->OnDeath.AddDynamic(this, &ATHRPlayerCharacter::HandleDeath);
	}

	/* 回復薬を満タンで開始 */
	PotionsRemaining = MaxPotions;
}

void ATHRPlayerCharacter::HandleDeath()
{
	/* 入力と移動を遮断し、少し待ってからレベルを再開する */
	if (APlayerController* PC = Cast<APlayerController>(GetController()))
	{
		DisableInput(PC);
	}
	GetCharacterMovement()->DisableMovement();

	if (GEngine)
	{
		GEngine->AddOnScreenDebugMessage(3, 3.0f, FColor::Red, TEXT("YOU DIED"));
	}

	FTimerHandle RestartTimerHandle;
	GetWorldTimerManager().SetTimer(
		RestartTimerHandle,
		FTimerDelegate::CreateWeakLambda(this, [this]()
		{
			UGameplayStatics::OpenLevel(this, FName(*UGameplayStatics::GetCurrentLevelName(this)));
		}),
		RestartDelay, false);
}

/* ── 入力セットアップ ──────────────────────────────────────── */

void ATHRPlayerCharacter::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);

	UEnhancedInputComponent* EIC = Cast<UEnhancedInputComponent>(PlayerInputComponent);
	if (!EIC)
	{
		UE_LOG(LogTheHollowRite, Error, TEXT("[%s] EnhancedInputComponent が見つかりません"), *GetNameSafe(this));
		return;
	}

	EIC->BindAction(MoveAction, ETriggerEvent::Triggered, this, &ATHRPlayerCharacter::HandleMove);
	EIC->BindAction(MoveAction, ETriggerEvent::Completed, this, &ATHRPlayerCharacter::HandleMove);
	EIC->BindAction(LookAction, ETriggerEvent::Triggered, this, &ATHRPlayerCharacter::HandleLook);
	EIC->BindAction(DodgeAction, ETriggerEvent::Started, this, &ATHRPlayerCharacter::HandleDodge);
	EIC->BindAction(AttackAction, ETriggerEvent::Started, this, &ATHRPlayerCharacter::HandleAttack);

	/* R: 回復薬使用 */
	PlayerInputComponent->BindKey(EKeys::R, IE_Pressed, this, &ATHRPlayerCharacter::HandlePotion);

	/* 1 / 2: 武器選択（セイバー / ブラスター） */
	PlayerInputComponent->BindKey(EKeys::One, IE_Pressed, this, &ATHRPlayerCharacter::SelectSaber);
	PlayerInputComponent->BindKey(EKeys::Two, IE_Pressed, this, &ATHRPlayerCharacter::SelectBlaster);

	/* F1: 統合デバッグ表示のトグル（THR.Debug を反転 — 判定構体＋ボスパネル） */
	FInputKeyBinding DebugToggleBinding(FInputChord(EKeys::F1), IE_Pressed);
	DebugToggleBinding.KeyDelegate.GetDelegateForManualSet().BindLambda([]()
	{
		static IConsoleVariable* DebugCVar =
			IConsoleManager::Get().FindConsoleVariable(TEXT("THR.Debug"));
		if (DebugCVar)
		{
			const int32 NewValue = (DebugCVar->GetInt() == 0) ? 1 : 0;
			DebugCVar->Set(NewValue);
			if (GEngine)
			{
				GEngine->AddOnScreenDebugMessage(5, 1.5f, FColor::Yellow,
					NewValue ? TEXT("DEBUG: ON") : TEXT("DEBUG: OFF"));
			}
		}
	});
	PlayerInputComponent->KeyBindings.Add(DebugToggleBinding);

	/* F3 / F4: デバッグ — ボスをフェーズ2 / 3 へ強制遷移 */
	PlayerInputComponent->BindKey(EKeys::F3, IE_Pressed, this, &ATHRPlayerCharacter::DebugForcePhase2);
	PlayerInputComponent->BindKey(EKeys::F4, IE_Pressed, this, &ATHRPlayerCharacter::DebugForcePhase3);
}

void ATHRPlayerCharacter::DebugForcePhase2()
{
	if (ATHRBossCharacter* Boss = Cast<ATHRBossCharacter>(
			UGameplayStatics::GetActorOfClass(GetWorld(), ATHRBossCharacter::StaticClass())))
	{
		Boss->DebugSetPhase(2);
		if (GEngine) { GEngine->AddOnScreenDebugMessage(9, 1.5f, FColor::Magenta, TEXT("DEBUG: FORCE PHASE 2")); }
	}
}

void ATHRPlayerCharacter::DebugForcePhase3()
{
	if (ATHRBossCharacter* Boss = Cast<ATHRBossCharacter>(
			UGameplayStatics::GetActorOfClass(GetWorld(), ATHRBossCharacter::StaticClass())))
	{
		Boss->DebugSetPhase(3);
		if (GEngine) { GEngine->AddOnScreenDebugMessage(9, 1.5f, FColor::Magenta, TEXT("DEBUG: FORCE PHASE 3")); }
	}
}

/* ── 入力ハンドラ ──────────────────────────────────────────── */

void ATHRPlayerCharacter::HandleMove(const FInputActionValue& Value)
{
	LastMoveInput = Value.Get<FVector2D>();

	if (Controller == nullptr)
	{
		return;
	}

	/* カメラ（コントローラ）のYaw基準で前後左右を決定 */
	const FRotator YawRotation(0.0f, GetControlRotation().Yaw, 0.0f);
	const FVector Forward = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::X);
	const FVector Right = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::Y);

	AddMovementInput(Forward, LastMoveInput.Y);
	AddMovementInput(Right, LastMoveInput.X);
}

void ATHRPlayerCharacter::HandleLook(const FInputActionValue& Value)
{
	const FVector2D Input = Value.Get<FVector2D>();
	AddControllerYawInput(Input.X);
	AddControllerPitchInput(-Input.Y);
}

void ATHRPlayerCharacter::HandleDodge()
{
	if (!bCanDodge || bIsDodging)
	{
		return;
	}

	bIsDodging = true;
	bCanDodge = false;

	/* 無敵フレームを付与 */
	if (HealthComponent)
	{
		HealthComponent->SetInvulnerable(true);
	}

	/* 回避方向へ瞬間的に射出 */
	LaunchCharacter(GetDodgeDirection() * DodgeImpulse, true, false);

	GetWorldTimerManager().SetTimer(
		DodgeTimerHandle, this, &ATHRPlayerCharacter::EndDodge, DodgeDuration, false);
}

void ATHRPlayerCharacter::EndDodge()
{
	bIsDodging = false;

	if (HealthComponent)
	{
		HealthComponent->SetInvulnerable(false);
	}

	/* クールダウン経過後に再び回避可能にする */
	GetWorldTimerManager().SetTimer(
		DodgeCooldownTimerHandle,
		FTimerDelegate::CreateWeakLambda(this, [this]() { bCanDodge = true; }),
		DodgeCooldown, false);
}

void ATHRPlayerCharacter::HandleAttack()
{
	if (bIsDodging)
	{
		return;
	}

	/* スワップ中・回避中は攻撃不可。武器ごとに独立したクールダウンを実時間で判定。 */
	if (bSwapping)
	{
		return;
	}

	const float Now = GetWorld()->GetTimeSeconds();
	if (CurrentWeapon == ETHRWeaponType::Saber)
	{
		if (Now - LastSaberTime < AttackCooldown)
		{
			return;
		}
		LastSaberTime = Now;
		PerformSaberAttack();
	}
	else
	{
		if (Now - LastBlasterTime < BlasterCooldown)
		{
			return;
		}
		LastBlasterTime = Now;
		PerformBlasterAttack();
	}
}

void ATHRPlayerCharacter::PerformSaberAttack()
{
	if (AttackMontage)
	{
		/* モンタージュ攻撃：当たり判定はモンタージュ内の THR Melee Hit 通知が
		   MeleeHitbox を開閉して行う（ボスと同一システム） */
		PlayAnimMontage(AttackMontage);
		return;
	}

	/* フォールバック：モンタージュ未設定時は即時スフィア判定 */
	const FVector AttackCenter = GetActorLocation() + GetActorForwardVector() * (AttackRange * 0.5f);

	static IConsoleVariable* DebugCVar = IConsoleManager::Get().FindConsoleVariable(TEXT("THR.Debug"));
	if (DebugCVar && DebugCVar->GetInt() != 0)
	{
		DrawDebugSphere(GetWorld(), AttackCenter, AttackRadius, 12, FColor::Cyan, false, 0.25f);
	}

	FCollisionObjectQueryParams ObjectParams;
	ObjectParams.AddObjectTypesToQuery(ECC_Pawn);

	FCollisionQueryParams QueryParams;
	QueryParams.AddIgnoredActor(this);

	TArray<FOverlapResult> Overlaps;
	GetWorld()->OverlapMultiByObjectType(
		Overlaps, AttackCenter, FQuat::Identity, ObjectParams,
		FCollisionShape::MakeSphere(AttackRadius), QueryParams);

	TSet<AActor*> DamagedActors;
	for (const FOverlapResult& Overlap : Overlaps)
	{
		AActor* HitActor = Overlap.GetActor();
		if (!HitActor || DamagedActors.Contains(HitActor))
		{
			continue;
		}
		if (UTHRHealthComponent* TargetHealth = HitActor->FindComponentByClass<UTHRHealthComponent>())
		{
			TargetHealth->ApplyDamage(AttackDamage);
			DamagedActors.Add(HitActor);
		}
	}
}

void ATHRPlayerCharacter::PerformBlasterAttack()
{
	if (ProjectileClass == nullptr)
	{
		return;
	}

	/* キャラクターの正面方向へ水平発射（カメラではなくキャラの向き基準） */
	const FRotator AimRotation(0.0f, GetActorRotation().Yaw, 0.0f);
	const FVector MuzzleLocation = GetActorLocation()
		+ AimRotation.Vector() * MuzzleForwardOffset
		+ FVector(0.0f, 0.0f, MuzzleHeight);

	FActorSpawnParameters Params;
	Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
	Params.Owner = this;
	Params.Instigator = this;

	if (ATHRProjectile* Projectile = GetWorld()->SpawnActor<ATHRProjectile>(
			ProjectileClass, MuzzleLocation, AimRotation, Params))
	{
		Projectile->InitProjectile(BlasterDamage, this);
	}
}

void ATHRPlayerCharacter::SelectSaber()
{
	BeginSwap(ETHRWeaponType::Saber);
}

void ATHRPlayerCharacter::SelectBlaster()
{
	BeginSwap(ETHRWeaponType::Blaster);
}

void ATHRPlayerCharacter::BeginSwap(ETHRWeaponType NewWeapon)
{
	/* 同じ武器・スワップ中は無視 */
	if (bSwapping || CurrentWeapon == NewWeapon)
	{
		return;
	}

	bSwapping = true;
	PendingWeapon = NewWeapon;

	if (GEngine)
	{
		GEngine->AddOnScreenDebugMessage(8, SwapTime, FColor::Cyan, TEXT("SWAPPING..."));
	}

	GetWorldTimerManager().SetTimer(
		SwapTimerHandle, this, &ATHRPlayerCharacter::FinishSwap, SwapTime, false);
}

void ATHRPlayerCharacter::FinishSwap()
{
	CurrentWeapon = PendingWeapon;
	bSwapping = false;
}

void ATHRPlayerCharacter::HandlePotion()
{
	if (PotionsRemaining <= 0 || HealthComponent == nullptr || HealthComponent->IsDead())
	{
		return;
	}
	if (HealthComponent->GetHealth() >= HealthComponent->GetMaxHealth())
	{
		return; /* 満タン時は消費しない */
	}

	--PotionsRemaining;
	HealthComponent->Heal(PotionHealAmount);

	if (GEngine)
	{
		GEngine->AddOnScreenDebugMessage(6, 1.5f, FColor::Green,
			FString::Printf(TEXT("POTION USED (%d left)"), PotionsRemaining));
	}
}

FVector ATHRPlayerCharacter::GetDodgeDirection() const
{
	if (!LastMoveInput.IsNearlyZero())
	{
		const FRotator YawRotation(0.0f, GetControlRotation().Yaw, 0.0f);
		const FVector Forward = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::X);
		const FVector Right = FRotationMatrix(YawRotation).GetUnitAxis(EAxis::Y);
		return (Forward * LastMoveInput.Y + Right * LastMoveInput.X).GetSafeNormal();
	}

	/* 移動入力がなければ正面へ回避 */
	return GetActorForwardVector();
}
