/// The Hollow Rite - メインメニュー用プレイヤーコントローラ

#include "THRMainMenuPlayerController.h"
#include "Kismet/GameplayStatics.h"
#include "Kismet/KismetSystemLibrary.h"

void ATHRMainMenuPlayerController::SetupInputComponent()
{
	Super::SetupInputComponent();

	if (InputComponent)
	{
		InputComponent->BindKey(EKeys::Enter, IE_Pressed, this, &ATHRMainMenuPlayerController::HandleStart);
		InputComponent->BindKey(EKeys::Q, IE_Pressed, this, &ATHRMainMenuPlayerController::HandleQuit);
		InputComponent->BindKey(EKeys::Escape, IE_Pressed, this, &ATHRMainMenuPlayerController::HandleQuit);
	}
}

void ATHRMainMenuPlayerController::HandleStart()
{
	UGameplayStatics::OpenLevel(this, BattleLevelName);
}

void ATHRMainMenuPlayerController::HandleQuit()
{
	UKismetSystemLibrary::QuitGame(this, this, EQuitPreference::Quit, false);
}
