/// The Hollow Rite - ゲームモード

#include "THRGameMode.h"
#include "Player/THRPlayerCharacter.h"
#include "UI/THRBossHUD.h"
#include "GameFramework/PlayerController.h"

ATHRGameMode::ATHRGameMode()
{
	DefaultPawnClass = ATHRPlayerCharacter::StaticClass();
	HUDClass = ATHRBossHUD::StaticClass();
}

void ATHRGameMode::BeginPlay()
{
	Super::BeginPlay();

	/* 一人称ゲームプレイ用にマウスをビューポートにロック */
	if (APlayerController* PC = GetWorld()->GetFirstPlayerController())
	{
		PC->SetShowMouseCursor(false);
		PC->SetInputMode(FInputModeGameOnly());
	}
}
