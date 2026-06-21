/// The Hollow Rite - ボス戦HUD（ボス名・体力バーを画面上部に描画）

#include "THRBossHUD.h"
#include "Boss/THRBossCharacter.h"
#include "Boss/THRBossAttackDef.h"
#include "Player/THRPlayerCharacter.h"
#include "Combat/THRHealthComponent.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"
#include "HAL/IConsoleManager.h"
#include "Kismet/GameplayStatics.h"

void ATHRBossHUD::DrawHUD()
{
	Super::DrawHUD();

	if (Canvas == nullptr)
	{
		return;
	}

	const UTHRHealthComponent* Health = ResolveBossHealth();
	if (Health == nullptr)
	{
		return;
	}

	const float ScreenWidth = Canvas->SizeX;
	const float BarWidth = ScreenWidth * BarWidthRatio;
	const float BarX = (ScreenWidth - BarWidth) * 0.5f;

	UFont* NameFont = GEngine->GetLargeFont();
	UFont* SmallFont = GEngine->GetSmallFont();

	/* ボス名（バーの上、中央揃え） */
	const FString NameStr = BossName.ToString();
	float NameWidth = 0.0f, NameHeight = 0.0f;
	GetTextSize(NameStr, NameWidth, NameHeight, NameFont, 1.0f);
	DrawText(NameStr, FLinearColor::White, (ScreenWidth - NameWidth) * 0.5f, TopOffset, NameFont, 1.0f);

	const float BarY = TopOffset + NameHeight + 6.0f;
	const float Percent = Health->GetHealthPercent();

	/* 外枠 → 空バー（暗赤）→ 残量（FillColor） */
	DrawRect(FLinearColor(0.0f, 0.0f, 0.0f, 0.85f), BarX - 2.0f, BarY - 2.0f, BarWidth + 4.0f, BarHeight + 4.0f);
	DrawRect(FLinearColor(0.12f, 0.0f, 0.0f, 0.9f), BarX, BarY, BarWidth, BarHeight);
	DrawRect(FillColor, BarX, BarY, BarWidth * Percent, BarHeight);

	/* HP数値（バーの下、中央揃え） */
	const FString HpStr = FString::Printf(TEXT("%.0f / %.0f"), Health->GetHealth(), Health->GetMaxHealth());
	float HpWidth = 0.0f, HpHeight = 0.0f;
	GetTextSize(HpStr, HpWidth, HpHeight, SmallFont, 1.0f);
	DrawText(HpStr, FLinearColor::White, (ScreenWidth - HpWidth) * 0.5f, BarY + BarHeight + 3.0f, SmallFont, 1.0f);

	/* ── プレイヤーHPバー（左下） ─────────────────────────── */

	const APawn* PlayerPawn = GetOwningPawn();
	const UTHRHealthComponent* PlayerHealth =
		PlayerPawn ? PlayerPawn->FindComponentByClass<UTHRHealthComponent>() : nullptr;
	if (PlayerHealth == nullptr)
	{
		return;
	}

	/* 配置定数（グレイボックス段階。最終UIはUMG移行時に再設計） */
	constexpr float PlayerBarWidth = 260.0f;
	constexpr float PlayerBarHeight = 14.0f;
	constexpr float PlayerBarMarginX = 40.0f;
	constexpr float PlayerBarMarginY = 56.0f;
	const FLinearColor PlayerFillColor(0.0f, 1.0f, 0.9f, 1.0f); /* 電脳世界のシグネチャ色（シアン） */

	const float PlayerBarX = PlayerBarMarginX;
	const float PlayerBarY = Canvas->SizeY - PlayerBarMarginY;
	const float PlayerPercent = PlayerHealth->GetHealthPercent();

	DrawRect(FLinearColor(0.0f, 0.0f, 0.0f, 0.85f),
		PlayerBarX - 2.0f, PlayerBarY - 2.0f, PlayerBarWidth + 4.0f, PlayerBarHeight + 4.0f);
	DrawRect(FLinearColor(0.0f, 0.10f, 0.09f, 0.9f), PlayerBarX, PlayerBarY, PlayerBarWidth, PlayerBarHeight);
	DrawRect(PlayerFillColor, PlayerBarX, PlayerBarY, PlayerBarWidth * PlayerPercent, PlayerBarHeight);

	const FString PlayerHpStr = FString::Printf(
		TEXT("HP %.0f / %.0f"), PlayerHealth->GetHealth(), PlayerHealth->GetMaxHealth());
	DrawText(PlayerHpStr, FLinearColor::White, PlayerBarX, PlayerBarY - 18.0f, SmallFont, 1.0f);

	/* 回復薬の残数（HPバーの下、[R]キー使用） */
	if (const ATHRPlayerCharacter* Player = Cast<ATHRPlayerCharacter>(PlayerPawn))
	{
		const FString PotionStr = FString::Printf(TEXT("[R] POTION x %d"), Player->GetPotionsRemaining());
		const FLinearColor PotionColor = Player->GetPotionsRemaining() > 0
			? FLinearColor(0.4f, 1.0f, 0.4f, 1.0f)
			: FLinearColor(0.4f, 0.4f, 0.4f, 1.0f);
		DrawText(PotionStr, PotionColor, PlayerBarX, PlayerBarY + PlayerBarHeight + 6.0f, SmallFont, 1.0f);

		/* 現在の武器（スワップ中は遷移先を点滅表示） */
		FString WeaponStr;
		FLinearColor WeaponColor;
		if (Player->IsSwapping())
		{
			const TCHAR* Pending = Player->GetPendingWeapon() == ETHRWeaponType::Saber ? TEXT("SABER") : TEXT("BLASTER");
			WeaponStr = FString::Printf(TEXT("[1/2] SWAPPING -> %s"), Pending);
			WeaponColor = FLinearColor(1.0f, 0.85f, 0.3f, 1.0f);
		}
		else
		{
			const TCHAR* Cur = Player->GetCurrentWeapon() == ETHRWeaponType::Saber ? TEXT("SABER") : TEXT("BLASTER");
			WeaponStr = FString::Printf(TEXT("[1/2] WEAPON: %s"), Cur);
			WeaponColor = FLinearColor(0.0f, 1.0f, 0.9f, 1.0f);
		}
		DrawText(WeaponStr, WeaponColor, PlayerBarX, PlayerBarY + PlayerBarHeight + 22.0f, SmallFont, 1.0f);
	}

	/* ── ボス状態デバッグパネル（F1 統合デバッグトグル） ────── */

	static IConsoleVariable* DebugCVar = IConsoleManager::Get().FindConsoleVariable(TEXT("THR.Debug"));
	if (DebugCVar == nullptr || DebugCVar->GetInt() == 0)
	{
		return;
	}

	ATHRBossCharacter* Boss = CachedBoss.Get();
	if (Boss == nullptr)
	{
		return;
	}

	const int32 PhaseInt = Boss->GetPhaseAsInt();
	const bool bDead = Boss->CurrentPhase == ETHRBossPhase::Dead;

	/* フェーズごとのシグネチャ色 */
	FLinearColor PhaseColor;
	FString PhaseStr;
	if (bDead)               { PhaseColor = FLinearColor(0.5f, 0.5f, 0.5f, 1.0f); PhaseStr = TEXT("DEAD"); }
	else if (PhaseInt == 1)  { PhaseColor = FLinearColor(0.0f, 1.0f, 0.9f, 1.0f); PhaseStr = TEXT("P1"); }
	else if (PhaseInt == 2)  { PhaseColor = FLinearColor(0.61f, 0.3f, 1.0f, 1.0f); PhaseStr = TEXT("P2"); }
	else                     { PhaseColor = FLinearColor(1.0f, 0.22f, 0.31f, 1.0f); PhaseStr = TEXT("P3"); }

	const float PanelX = Canvas->SizeX - 270.0f;
	float Y = 80.0f;
	const float LineH = 16.0f;

	/* 背景パネル */
	const float PanelH = 130.0f + Boss->AttackTable.Num() * LineH;
	DrawRect(FLinearColor(0.0f, 0.0f, 0.0f, 0.7f), PanelX - 10.0f, Y - 10.0f, 260.0f, PanelH);

	DrawText(TEXT("== BOSS DEBUG =="), FLinearColor::Yellow, PanelX, Y, SmallFont, 1.0f);
	Y += LineH * 1.5f;

	/* フェーズ（大きめ・色付き） */
	DrawText(FString::Printf(TEXT("PHASE: %s"), *PhaseStr), PhaseColor, PanelX, Y, NameFont, 1.0f);
	Y += LineH * 1.8f;

	/* HP と割合 */
	const float Pct = Health->GetHealthPercent() * 100.0f;
	DrawText(FString::Printf(TEXT("HP: %.0f / %.0f  (%.1f%%)"),
		Health->GetHealth(), Health->GetMaxHealth(), Pct),
		FLinearColor::White, PanelX, Y, SmallFont, 1.0f);
	Y += LineH;

	DrawText(TEXT("Thresholds: P2<=66%  P3<=33%"),
		FLinearColor(0.7f, 0.7f, 0.7f, 1.0f), PanelX, Y, SmallFont, 1.0f);
	Y += LineH;

	DrawText(TEXT("[F3/F4] force phase 2/3"),
		FLinearColor(0.7f, 0.7f, 0.4f, 1.0f), PanelX, Y, SmallFont, 1.0f);
	Y += LineH;

	/* プレイヤーとの距離 */
	const float Dist = FVector::Dist(Boss->GetActorLocation(), PlayerPawn->GetActorLocation());
	DrawText(FString::Printf(TEXT("Dist: %.0f"), Dist),
		FLinearColor::White, PanelX, Y, SmallFont, 1.0f);
	Y += LineH * 1.5f;

	/* 攻撃テーブル：フェーズ解禁・距離の可否を可視化 */
	DrawText(TEXT("-- ATTACKS --"), FLinearColor(0.7f, 0.7f, 0.7f, 1.0f), PanelX, Y, SmallFont, 1.0f);
	Y += LineH;

	for (const FTHRBossAttackDef& Def : Boss->AttackTable)
	{
		const bool bPhaseOk = Def.MinPhase <= PhaseInt;
		const bool bRangeOk = Dist >= Def.MinRange && Dist <= Def.MaxRange;

		FLinearColor RowColor;
		FString StatusStr;
		if (!bPhaseOk)      { RowColor = FLinearColor(0.45f, 0.45f, 0.45f, 1.0f); StatusStr = TEXT("LOCK"); }
		else if (bRangeOk)  { RowColor = FLinearColor(0.4f, 1.0f, 0.4f, 1.0f);    StatusStr = TEXT("READY"); }
		else                { RowColor = FLinearColor(1.0f, 0.85f, 0.3f, 1.0f);   StatusStr = TEXT("range"); }

		DrawText(FString::Printf(TEXT("%-12s P%d  %s"), *Def.AttackName.ToString(), Def.MinPhase, *StatusStr),
			RowColor, PanelX, Y, SmallFont, 1.0f);
		Y += LineH;
	}
}

UTHRHealthComponent* ATHRBossHUD::ResolveBossHealth()
{
	ATHRBossCharacter* Boss = CachedBoss.Get();
	if (Boss == nullptr)
	{
		/* レベル内の唯一のボスを検索してキャッシュ */
		Boss = Cast<ATHRBossCharacter>(
			UGameplayStatics::GetActorOfClass(GetWorld(), ATHRBossCharacter::StaticClass()));
		CachedBoss = Boss;
	}

	return Boss ? Boss->FindComponentByClass<UTHRHealthComponent>() : nullptr;
}
