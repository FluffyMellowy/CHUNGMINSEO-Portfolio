/// The Hollow Rite - ボスキャラクター基底クラス

#include "THRBossCharacter.h"
#include "THRBossAIController.h"
#include "AOE/THRAOEActor.h"
#include "Bullets/THRBulletPoolSubsystem.h"
#include "Combat/THRHealthComponent.h"
#include "Combat/THRMeleeHitboxComponent.h"
#include "Components/CapsuleComponent.h"
#include "Components/SkeletalMeshComponent.h"
#include "Engine/SkeletalMesh.h"
#include "Animation/AnimInstance.h"
#include "Animation/AnimSequence.h"
#include "BrainComponent.h"
#include "Engine/Engine.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Kismet/GameplayStatics.h"
#include "UObject/ConstructorHelpers.h"
#include "TheHollowRite.h"

ATHRBossCharacter::ATHRBossCharacter()
{
	PrimaryActorTick.bCanEverTick = false;

	/* AIコントローラ（ポーズ時に BossBehaviorTree を起動する） */
	AIControllerClass = ATHRBossAIController::StaticClass();
	AutoPossessAI = EAutoPossessAI::PlacedInWorldOrSpawned;

	/* 移動・回転：コントローラの Focus 方向へ滑らかに向き直る */
	bUseControllerRotationYaw = false;
	UCharacterMovementComponent* CMC = GetCharacterMovement();
	CMC->bUseControllerDesiredRotation = true;
	CMC->bOrientRotationToMovement = false;
	CMC->RotationRate = FRotator(0.0f, 240.0f, 0.0f);
	CMC->MaxWalkSpeed = MoveSpeed;

	/* AI経路移動でも加速度を発生させる。
	   これが false だと ABP のロコモーションが「移動していない」と判定し、
	   ボスが滑るように移動する（テンプレートABPは加速度で移動判定するため）。 */
	if (FNavMovementProperties* NavProps = CMC->GetNavMovementProperties())
	{
		NavProps->bUseAccelerationForPaths = true;
	}

	/* 体力コンポーネント */
	HealthComponent = CreateDefaultSubobject<UTHRHealthComponent>(TEXT("HealthComponent"));

	/* 近接攻撃ヒットボックス（アニメ通知 THR Melee Hit から開閉） */
	MeleeHitbox = CreateDefaultSubobject<UTHRMeleeHitboxComponent>(TEXT("MeleeHitbox"));

	/* スケルタルメッシュ（マネキン Quinn）とアニメーションBPを割り当て */
	USkeletalMeshComponent* MeshComp = GetMesh();
	MeshComp->SetRelativeLocationAndRotation(FVector(0.0f, 0.0f, -88.0f), FRotator(0.0f, -90.0f, 0.0f));

	static ConstructorHelpers::FObjectFinder<USkeletalMesh> BossMeshFinder(
		TEXT("/Game/Characters/Mannequins/Meshes/SKM_Quinn_Simple.SKM_Quinn_Simple"));
	if (BossMeshFinder.Succeeded())
	{
		MeshComp->SetSkeletalMeshAsset(BossMeshFinder.Object);
	}

	static ConstructorHelpers::FClassFinder<UAnimInstance> BossAnimFinder(
		TEXT("/Game/Characters/Mannequins/Anims/Unarmed/ABP_Unarmed"));
	if (BossAnimFinder.Succeeded())
	{
		MeshComp->SetAnimInstanceClass(BossAnimFinder.Class);
	}

	/* 死亡アニメーションのデフォルト */
	static ConstructorHelpers::FObjectFinder<UAnimSequence> DeathAnimFinder(
		TEXT("/Game/Characters/Mannequins/Anims/Death/MM_Death_Front_01.MM_Death_Front_01"));
	if (DeathAnimFinder.Succeeded())
	{
		DeathAnim = DeathAnimFinder.Object;
	}
}

void ATHRBossCharacter::BeginPlay()
{
	Super::BeginPlay();

	/* 最大体力を設定し、死亡・体力変化デリゲートを購読 */
	if (HealthComponent)
	{
		HealthComponent->SetMaxHealth(BossMaxHealth, true);
		HealthComponent->OnDeath.AddDynamic(this, &ATHRBossCharacter::HandleDeath);
		HealthComponent->OnHealthChanged.AddDynamic(this, &ATHRBossCharacter::HandleHealthChanged);
	}
}

int32 ATHRBossCharacter::GetPhaseAsInt() const
{
	switch (CurrentPhase)
	{
	case ETHRBossPhase::Phase1: return 1;
	case ETHRBossPhase::Phase2: return 2;
	case ETHRBossPhase::Phase3: return 3;
	default: return 3;
	}
}

void ATHRBossCharacter::DebugSetPhase(int32 PhaseNum)
{
	if (HealthComponent == nullptr || HealthComponent->IsDead())
	{
		return;
	}

	/* 目標フェーズの体力割合（P2=65%, P3=32%）まで下げて、既存の遷移ロジックを発動する */
	const float Ratio = (PhaseNum >= 3) ? 0.32f : 0.65f;
	const float TargetHP = HealthComponent->GetMaxHealth() * Ratio;
	const float CurrentHP = HealthComponent->GetHealth();
	if (CurrentHP > TargetHP)
	{
		HealthComponent->ApplyDamage(CurrentHP - TargetHP);
	}
}

bool ATHRBossCharacter::IsAttackOnCooldown(const FTHRBossAttackDef& Def) const
{
	const float* LastTime = LastAttackTimes.Find(Def.AttackName);
	if (LastTime == nullptr)
	{
		return false;
	}

	/* フェーズが進むほどクールダウンが短くなる */
	float CooldownMult = 1.0f;
	if (CurrentPhase == ETHRBossPhase::Phase2)
	{
		CooldownMult = Phase2CooldownMult;
	}
	else if (CurrentPhase == ETHRBossPhase::Phase3)
	{
		CooldownMult = Phase3CooldownMult;
	}

	return (GetWorld()->GetTimeSeconds() - *LastTime) < Def.Cooldown * CooldownMult;
}

float ATHRBossCharacter::PlayAttack(const FTHRBossAttackDef& Def)
{
	if (Def.Montage == nullptr || CurrentPhase == ETHRBossPhase::Dead)
	{
		return 0.0f;
	}

	const float Duration = PlayAnimMontage(Def.Montage);
	if (Duration > 0.0f)
	{
		LastAttackTimes.Add(Def.AttackName, GetWorld()->GetTimeSeconds());

		/* 突進攻撃：ターゲット方向へインパルスを与える */
		if (Def.LungeImpulse > 0.0f)
		{
			if (const APawn* Target = UGameplayStatics::GetPlayerPawn(this, 0))
			{
				FVector Direction = Target->GetActorLocation() - GetActorLocation();
				Direction.Z = 0.0f;
				LaunchCharacter(Direction.GetSafeNormal() * Def.LungeImpulse, true, false);
			}
		}

		/* チャネルB：定義された範囲攻撃をスケジュール */
		for (const FTHRAOESpawnDef& SpawnDef : Def.AOESpawns)
		{
			if (SpawnDef.DelayFromAttackStart <= 0.0f)
			{
				SpawnAOE(SpawnDef);
			}
			else
			{
				FTimerHandle SpawnTimerHandle;
				GetWorldTimerManager().SetTimer(
					SpawnTimerHandle,
					FTimerDelegate::CreateWeakLambda(this, [this, SpawnDef]() { SpawnAOE(SpawnDef); }),
					SpawnDef.DelayFromAttackStart, false);
			}
		}

		/* チャネルC：定義された弾幕パターンをスケジュール */
		for (const FTHRBulletPatternDef& Pattern : Def.BulletPatterns)
		{
			FTimerHandle BulletTimerHandle;
			GetWorldTimerManager().SetTimer(
				BulletTimerHandle,
				FTimerDelegate::CreateWeakLambda(this, [this, Pattern]() { FireBulletPattern(Pattern); }),
				FMath::Max(0.01f, Pattern.DelayFromAttackStart), false);
		}
	}
	return Duration;
}

void ATHRBossCharacter::FireBulletPattern(const FTHRBulletPatternDef& Pattern)
{
	if (CurrentPhase == ETHRBossPhase::Dead)
	{
		return;
	}

	UTHRBulletPoolSubsystem* Pool = GetWorld()->GetSubsystem<UTHRBulletPoolSubsystem>();
	if (Pool == nullptr)
	{
		return;
	}

	/* 発射原点：ボスの胴体高さ付近 */
	const FVector Origin = GetActorLocation() + FVector(0.0f, 0.0f, 40.0f);
	APawn* Target = UGameplayStatics::GetPlayerPawn(this, 0);
	Pool->FirePattern(Pattern, Origin, Target);
}

void ATHRBossCharacter::SpawnAOE(const FTHRAOESpawnDef& SpawnDef)
{
	if (CurrentPhase == ETHRBossPhase::Dead)
	{
		return;
	}

	/* アンカーに応じてスポーン位置・向きを解決する */
	FVector Location = GetActorLocation();
	FRotator Rotation = GetActorRotation();

	switch (SpawnDef.Anchor)
	{
	case ETHRAOEAnchor::Target:
		if (const APawn* Target = UGameplayStatics::GetPlayerPawn(this, 0))
		{
			/* 詠唱開始時のプレイヤー位置スナップショット。向きはボス→プレイヤー方向 */
			Location = Target->GetActorLocation();
			FVector ToTarget = Location - GetActorLocation();
			ToTarget.Z = 0.0f;
			if (!ToTarget.IsNearlyZero())
			{
				Rotation = ToTarget.Rotation();
			}
		}
		break;

	case ETHRAOEAnchor::Offset:
		Location = GetActorTransform().TransformPosition(SpawnDef.Offset);
		break;

	case ETHRAOEAnchor::Self:
	default:
		break;
	}

	FActorSpawnParameters Params;
	Params.SpawnCollisionHandlingOverride = ESpawnActorCollisionHandlingMethod::AlwaysSpawn;
	Params.Owner = this;

	if (ATHRAOEActor* AOE = GetWorld()->SpawnActor<ATHRAOEActor>(Location, Rotation, Params))
	{
		AOE->InitAOE(SpawnDef.AOE, this);
	}
}

void ATHRBossCharacter::HandleHealthChanged(float NewHealth, float MaxHealth)
{
	if (CurrentPhase == ETHRBossPhase::Dead || MaxHealth <= 0.0f)
	{
		return;
	}

	/* 体力割合でフェーズを判定（66% / 33% で遷移） */
	const float Percent = NewHealth / MaxHealth;
	ETHRBossPhase NewPhase = CurrentPhase;

	if (Percent <= 0.33f)
	{
		NewPhase = ETHRBossPhase::Phase3;
	}
	else if (Percent <= 0.66f)
	{
		NewPhase = ETHRBossPhase::Phase2;
	}

	if (NewPhase != CurrentPhase)
	{
		CurrentPhase = NewPhase;
		const int32 PhaseNumber = (NewPhase == ETHRBossPhase::Phase3) ? 3 : 2;

		/* フェーズに応じて移動速度を加速 */
		const float SpeedMult = (NewPhase == ETHRBossPhase::Phase3) ? Phase3SpeedMult : Phase2SpeedMult;
		GetCharacterMovement()->MaxWalkSpeed = MoveSpeed * SpeedMult;

		UE_LOG(LogTheHollowRite, Log, TEXT("[Boss] フェーズ %d へ移行（HP %.0f%%、移動速度 x%.2f）"),
			PhaseNumber, Percent * 100.0f, SpeedMult);
		if (GEngine)
		{
			GEngine->AddOnScreenDebugMessage(4, 3.0f, FColor::Magenta,
				FString::Printf(TEXT("PHASE %d"), PhaseNumber));
		}
	}
}

void ATHRBossCharacter::HandleDeath()
{
	UE_LOG(LogTheHollowRite, Log, TEXT("[Boss] %s が撃破された"), *GetNameSafe(this));

	if (GEngine)
	{
		GEngine->AddOnScreenDebugMessage(2, 5.0f, FColor::Red, TEXT("BOSS DEFEATED"));
	}

	CurrentPhase = ETHRBossPhase::Dead;

	/* Behavior Tree を停止 */
	if (const AAIController* AIC = Cast<AAIController>(GetController()))
	{
		if (UBrainComponent* Brain = AIC->GetBrainComponent())
		{
			Brain->StopLogic(TEXT("Dead"));
		}
	}

	/* 移動とコリジョンを停止し、死亡アニメーションを再生 */
	GetCharacterMovement()->DisableMovement();
	GetCapsuleComponent()->SetCollisionEnabled(ECollisionEnabled::NoCollision);

	if (DeathAnim)
	{
		GetMesh()->PlayAnimation(DeathAnim, false);
	}
}
