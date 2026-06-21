/// The Hollow Rite - 近接攻撃ヒットボックスコンポーネント

#include "THRMeleeHitboxComponent.h"
#include "THRHealthComponent.h"
#include "Engine/OverlapResult.h"
#include "Engine/World.h"
#include "DrawDebugHelpers.h"
#include "HAL/IConsoleManager.h"

/// 統合デバッグ表示の切替コンソール変数（F1 トグル／コンソール「THR.Debug 1」）。
/// 判定構体・ボス状態パネル・AOE 形状などデバッグ表示全般を一括制御する。
static TAutoConsoleVariable<int32> CVarTHRDebug(
	TEXT("THR.Debug"),
	0,
	TEXT("統合デバッグ表示 (0=オフ, 1=オン)"));

UTHRMeleeHitboxComponent::UTHRMeleeHitboxComponent()
{
	PrimaryComponentTick.bCanEverTick = true;
	/* ウィンドウが開くまで Tick は不要 */
	PrimaryComponentTick.bStartWithTickEnabled = false;
}

void UTHRMeleeHitboxComponent::OpenWindow(float InDamage, float InRange, float InRadius)
{
	CurrentDamage = InDamage;
	CurrentRange = InRange;
	CurrentRadius = InRadius;
	HitActorsThisWindow.Reset();
	bWindowOpen = true;
	SetComponentTickEnabled(true);
}

void UTHRMeleeHitboxComponent::CloseWindow()
{
	bWindowOpen = false;
	SetComponentTickEnabled(false);
	HitActorsThisWindow.Reset();
}

void UTHRMeleeHitboxComponent::TickComponent(float DeltaTime, ELevelTick TickType,
	FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	if (!bWindowOpen)
	{
		return;
	}

	AActor* OwnerActor = GetOwner();
	if (OwnerActor == nullptr)
	{
		return;
	}

	/* 所有者の前方にスフィアを置いて、判定対象のポーンを検索 */
	const FVector Center = OwnerActor->GetActorLocation()
		+ OwnerActor->GetActorForwardVector() * (CurrentRange * 0.5f);

	FCollisionObjectQueryParams ObjectParams;
	ObjectParams.AddObjectTypesToQuery(ECC_Pawn);

	FCollisionQueryParams QueryParams;
	QueryParams.AddIgnoredActor(OwnerActor);

	TArray<FOverlapResult> Overlaps;
	GetWorld()->OverlapMultiByObjectType(
		Overlaps, Center, FQuat::Identity, ObjectParams,
		FCollisionShape::MakeSphere(CurrentRadius), QueryParams);

	for (const FOverlapResult& Overlap : Overlaps)
	{
		AActor* HitActor = Overlap.GetActor();
		if (HitActor == nullptr || HitActorsThisWindow.Contains(HitActor))
		{
			continue;
		}

		if (UTHRHealthComponent* TargetHealth = HitActor->FindComponentByClass<UTHRHealthComponent>())
		{
			TargetHealth->ApplyDamage(CurrentDamage);
			HitActorsThisWindow.Add(HitActor);
		}
	}

	/* Live Coding 後も安全なよう、コンソール変数はマネージャ経由で参照する
	   （TAutoConsoleVariable オブジェクト直接参照は Live Coding でポインタが無効化され得る） */
	static IConsoleVariable* DebugCVar = IConsoleManager::Get().FindConsoleVariable(TEXT("THR.Debug"));
	if (bDrawDebug || (DebugCVar && DebugCVar->GetInt() != 0))
	{
		DrawDebugSphere(GetWorld(), Center, CurrentRadius, 12, FColor::Red, false, 0.1f);
	}
}
