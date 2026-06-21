/// The Hollow Rite - 範囲攻撃テレグラフアクター

#include "THRAOEActor.h"
#include "Combat/THRHealthComponent.h"
#include "Components/DecalComponent.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "DrawDebugHelpers.h"
#include "EngineUtils.h"
#include "GameFramework/Pawn.h"
#include "HAL/IConsoleManager.h"
#include "UObject/ConstructorHelpers.h"
#include "TheHollowRite.h"

ATHRAOEActor::ATHRAOEActor()
{
	PrimaryActorTick.bCanEverTick = true;

	USceneComponent* Root = CreateDefaultSubobject<USceneComponent>(TEXT("Root"));
	SetRootComponent(Root);

	/* デカールは下向きに投影（床に貼る） */
	TelegraphDecal = CreateDefaultSubobject<UDecalComponent>(TEXT("TelegraphDecal"));
	TelegraphDecal->SetupAttachment(Root);
	TelegraphDecal->SetRelativeRotation(FRotator(-90.0f, 0.0f, 0.0f));
	TelegraphDecal->SetVisibility(false);

	/* テレグラフマテリアルのデフォルトロード */
	static ConstructorHelpers::FObjectFinder<UMaterialInterface> TelegraphMatFinder(
		TEXT("/Game/THR/Boss/M_THRTelegraph.M_THRTelegraph"));
	if (TelegraphMatFinder.Succeeded())
	{
		TelegraphMaterial = TelegraphMatFinder.Object;
	}
}

void ATHRAOEActor::InitAOE(const FTHRAOEDef& InDef, AActor* InInstigatorActor)
{
	Def = InDef;
	InstigatorActor = InInstigatorActor;

	/* 地面へスナップ。BeginPlay はスポーン中（InitAOE より前）に走るためここで行う。
	   ポーンに当たらないよう WorldStatic オブジェクトのみを対象にトレースする。 */
	FHitResult Hit;
	const FVector Start = GetActorLocation() + FVector(0, 0, 200.0f);
	const FVector End = GetActorLocation() - FVector(0, 0, 2000.0f);
	FCollisionObjectQueryParams ObjectParams;
	ObjectParams.AddObjectTypesToQuery(ECC_WorldStatic);
	FCollisionQueryParams Params;
	Params.AddIgnoredActor(this);
	if (GetWorld()->LineTraceSingleByObjectType(Hit, Start, End, ObjectParams, Params))
	{
		SetActorLocation(Hit.Location + FVector(0, 0, 2.0f));
	}

	SetupTelegraphDecal();
}

void ATHRAOEActor::BeginPlay()
{
	Super::BeginPlay();
}

void ATHRAOEActor::SetupTelegraphDecal()
{
	/* テレグラフマテリアルがあればデカールを使用（InitAOE から呼ばれる — Def 設定後） */
	if (TelegraphMaterial)
	{
		DecalMID = UMaterialInstanceDynamic::Create(TelegraphMaterial, this);
		TelegraphDecal->SetDecalMaterial(DecalMID);

		/* 形状に応じたデカールサイズ（DecalSize: X=投影深度, Y/Z=平面サイズ） */
		float HalfExtent = Def.OuterRadius;
		if (Def.Shape == ETHRAOEShape::Line)
		{
			HalfExtent = Def.Length; /* ラインは原点から前方なので余裕を持たせる */
		}
		else if (Def.Shape == ETHRAOEShape::Cross)
		{
			HalfExtent = Def.Length * 0.5f;
		}
		TelegraphDecal->DecalSize = FVector(200.0f, HalfExtent, HalfExtent);
		TelegraphDecal->SetVisibility(true);

		DecalMID->SetScalarParameterValue(TEXT("Shape"), static_cast<float>(Def.Shape));
		DecalMID->SetScalarParameterValue(TEXT("InnerRatio"),
			Def.OuterRadius > 0.0f ? Def.InnerRadius / Def.OuterRadius : 0.0f);
		DecalMID->SetScalarParameterValue(TEXT("AngleDeg"), Def.AngleDeg);
		DecalMID->SetScalarParameterValue(TEXT("AspectWidth"),
			Def.Length > 0.0f ? Def.Width / Def.Length : 0.25f);
		DecalMID->SetVectorParameterValue(TEXT("Color"), GetSeverityColor());
	}
}

void ATHRAOEActor::Tick(float DeltaSeconds)
{
	Super::Tick(DeltaSeconds);

	if (bDetonated)
	{
		return;
	}

	Elapsed += DeltaSeconds;
	const float Progress = FMath::Clamp(Elapsed / Def.TelegraphTime, 0.0f, 1.0f);

	if (DecalMID)
	{
		DecalMID->SetScalarParameterValue(TEXT("Progress"), Progress);
	}
	else
	{
		/* マテリアル未設定：デバッグ描画でテレグラフを表示（グレイボックス） */
		DrawTelegraphShape(Progress, GetSeverityColor());
	}

	if (Progress >= 1.0f)
	{
		Detonate();
	}
}

void ATHRAOEActor::Detonate()
{
	bDetonated = true;

	/* スナップショット判定：発動瞬間の位置で範囲内のポーンへダメージ。
	   回避無敵中の対象は HealthComponent 側で無効化される。 */
	int32 HitCount = 0;
	for (TActorIterator<APawn> It(GetWorld()); It; ++It)
	{
		APawn* Pawn = *It;
		if (Pawn == InstigatorActor.Get())
		{
			continue;
		}

		UTHRHealthComponent* Health = Pawn->FindComponentByClass<UTHRHealthComponent>();
		if (Health == nullptr)
		{
			continue;
		}

		if (IsPointInside(Pawn->GetActorLocation()))
		{
			Health->ApplyDamage(Def.Damage);
			++HitCount;
		}
	}

	/* 発動の視覚フィードバック：判定形状を一瞬強く描画
	   （デバッグCVar有効時は判定とデカールのズレ検証にも使う） */
	DrawTelegraphShape(1.0f, HitCount > 0 ? FLinearColor::Red : FLinearColor::White);

	UE_LOG(LogTheHollowRite, Verbose, TEXT("[AOE] 発動 Shape=%d Hit=%d"),
		static_cast<int32>(Def.Shape), HitCount);

	SetActorTickEnabled(false);
	SetLifeSpan(0.3f);
}

bool ATHRAOEActor::IsPointInside(const FVector& WorldPoint) const
{
	/* アクターのローカル空間で2D判定（前方 = +X） */
	const FVector Local = GetActorTransform().InverseTransformPosition(WorldPoint);
	const FVector2D P(Local.X, Local.Y);
	const float Dist = P.Size();

	switch (Def.Shape)
	{
	case ETHRAOEShape::Circle:
		return Dist <= Def.OuterRadius;

	case ETHRAOEShape::Donut:
		return Dist >= Def.InnerRadius && Dist <= Def.OuterRadius;

	case ETHRAOEShape::Cone:
	{
		if (Dist > Def.OuterRadius)
		{
			return false;
		}
		const float AngleToPoint = FMath::RadiansToDegrees(FMath::Atan2(FMath::Abs(P.Y), P.X));
		return AngleToPoint <= Def.AngleDeg * 0.5f;
	}

	case ETHRAOEShape::Line:
		return Local.X >= 0.0f && Local.X <= Def.Length && FMath::Abs(Local.Y) <= Def.Width * 0.5f;

	case ETHRAOEShape::Cross:
	{
		const float HalfLen = Def.Length * 0.5f;
		const float HalfWid = Def.Width * 0.5f;
		const bool bInForwardBar = FMath::Abs(Local.X) <= HalfLen && FMath::Abs(Local.Y) <= HalfWid;
		const bool bInSideBar = FMath::Abs(Local.Y) <= HalfLen && FMath::Abs(Local.X) <= HalfWid;
		return bInForwardBar || bInSideBar;
	}

	default:
		return false;
	}
}

FLinearColor ATHRAOEActor::GetSeverityColor() const
{
	switch (Def.Severity)
	{
	case ETHRAOESeverity::Danger: return FLinearColor(1.0f, 0.22f, 0.31f, 1.0f); /* 赤 #FF3850 */
	case ETHRAOESeverity::Lethal: return FLinearColor(1.0f, 0.84f, 0.0f, 1.0f);  /* 金 */
	default:                      return FLinearColor(0.0f, 1.0f, 0.9f, 1.0f);   /* シアン #00FFE5 */
	}
}

void ATHRAOEActor::DrawTelegraphShape(float Progress, const FLinearColor& Color) const
{
	const FColor DrawColor = Color.ToFColor(true);
	const FVector Center = GetActorLocation() + FVector(0, 0, 4.0f);
	const FVector Forward = GetActorForwardVector();
	const FVector Up = FVector::UpVector;
	UWorld* World = GetWorld();
	constexpr int32 Segments = 36;

	switch (Def.Shape)
	{
	case ETHRAOEShape::Circle:
		DrawDebugCircle(World, Center, Def.OuterRadius, Segments, DrawColor, false, -1.0f, 0, 3.0f,
			FVector::ForwardVector, FVector::RightVector, false);
		/* 進行度：内側から満ちる円 */
		DrawDebugCircle(World, Center, Def.OuterRadius * Progress, Segments, DrawColor, false, -1.0f, 0, 6.0f,
			FVector::ForwardVector, FVector::RightVector, false);
		break;

	case ETHRAOEShape::Donut:
		DrawDebugCircle(World, Center, Def.InnerRadius, Segments, DrawColor, false, -1.0f, 0, 3.0f,
			FVector::ForwardVector, FVector::RightVector, false);
		DrawDebugCircle(World, Center, Def.OuterRadius, Segments, DrawColor, false, -1.0f, 0, 3.0f,
			FVector::ForwardVector, FVector::RightVector, false);
		DrawDebugCircle(World, Center,
			FMath::Lerp(Def.InnerRadius, Def.OuterRadius, Progress), Segments, DrawColor, false, -1.0f, 0, 6.0f,
			FVector::ForwardVector, FVector::RightVector, false);
		break;

	case ETHRAOEShape::Cone:
	{
		const float HalfAngleRad = FMath::DegreesToRadians(Def.AngleDeg * 0.5f);
		DrawDebugCone(World, Center, Forward, Def.OuterRadius, HalfAngleRad, 0.01f, 24, DrawColor, false, -1.0f, 0, 3.0f);
		DrawDebugCone(World, Center, Forward, Def.OuterRadius * Progress, HalfAngleRad, 0.01f, 24, DrawColor, false, -1.0f, 0, 6.0f);
		break;
	}

	case ETHRAOEShape::Line:
	{
		const FVector BoxCenter = Center + Forward * (Def.Length * 0.5f);
		const FVector Extent(Def.Length * 0.5f, Def.Width * 0.5f, 5.0f);
		DrawDebugBox(World, BoxCenter, Extent, GetActorQuat(), DrawColor, false, -1.0f, 0, 3.0f);
		DrawDebugBox(World, Center + Forward * (Def.Length * Progress * 0.5f),
			FVector(Def.Length * Progress * 0.5f, Def.Width * 0.5f, 5.0f), GetActorQuat(), DrawColor, false, -1.0f, 0, 6.0f);
		break;
	}

	case ETHRAOEShape::Cross:
	{
		const FVector ExtentA(Def.Length * 0.5f, Def.Width * 0.5f, 5.0f);
		const FVector ExtentB(Def.Width * 0.5f, Def.Length * 0.5f, 5.0f);
		DrawDebugBox(World, Center, ExtentA, GetActorQuat(), DrawColor, false, -1.0f, 0, 3.0f);
		DrawDebugBox(World, Center, ExtentB, GetActorQuat(), DrawColor, false, -1.0f, 0, 3.0f);
		DrawDebugBox(World, Center, ExtentA * FVector(Progress, 1.0f, 1.0f), GetActorQuat(), DrawColor, false, -1.0f, 0, 6.0f);
		break;
	}

	default:
		break;
	}
}
