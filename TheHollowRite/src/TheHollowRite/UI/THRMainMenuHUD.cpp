/// The Hollow Rite - メインメニューHUD

#include "THRMainMenuHUD.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"
#include "Engine/Font.h"

void ATHRMainMenuHUD::DrawHUD()
{
	Super::DrawHUD();

	if (Canvas == nullptr)
	{
		return;
	}

	const float ScreenWidth = Canvas->SizeX;
	const float ScreenHeight = Canvas->SizeY;

	UFont* TitleFont = GEngine->GetLargeFont();
	UFont* GuideFont = GEngine->GetMediumFont();

	/* 背景：全面を黒で塗りつぶし */
	DrawRect(FLinearColor(0.01f, 0.01f, 0.02f, 1.0f), 0.0f, 0.0f, ScreenWidth, ScreenHeight);

	/* タイトル（中央やや上、拡大スケールで描画） */
	const FString TitleStr = GameTitle.ToString();
	constexpr float TitleScale = 3.0f;
	float TitleWidth = 0.0f, TitleHeight = 0.0f;
	GetTextSize(TitleStr, TitleWidth, TitleHeight, TitleFont, TitleScale);
	DrawText(TitleStr, FLinearColor(0.0f, 1.0f, 0.9f, 1.0f),
		(ScreenWidth - TitleWidth) * 0.5f, ScreenHeight * 0.32f, TitleFont, TitleScale);

	/* 操作ガイド（中央下） */
	const FString StartStr = TEXT("[ENTER]  START");
	const FString QuitStr = TEXT("[Q]  QUIT");

	float StartWidth = 0.0f, StartHeight = 0.0f;
	GetTextSize(StartStr, StartWidth, StartHeight, GuideFont, 1.5f);
	DrawText(StartStr, FLinearColor::White,
		(ScreenWidth - StartWidth) * 0.5f, ScreenHeight * 0.58f, GuideFont, 1.5f);

	float QuitWidth = 0.0f, QuitHeight = 0.0f;
	GetTextSize(QuitStr, QuitWidth, QuitHeight, GuideFont, 1.5f);
	DrawText(QuitStr, FLinearColor(0.6f, 0.6f, 0.6f, 1.0f),
		(ScreenWidth - QuitWidth) * 0.5f, ScreenHeight * 0.58f + StartHeight * 2.0f, GuideFont, 1.5f);
}
