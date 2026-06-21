/// The Hollow Rite - メインメニュー用ゲームモード

#include "THRMainMenuGameMode.h"
#include "THRMainMenuPlayerController.h"
#include "UI/THRMainMenuHUD.h"
#include "GameFramework/SpectatorPawn.h"

ATHRMainMenuGameMode::ATHRMainMenuGameMode()
{
	/* メニューでは操作キャラクター不要 */
	DefaultPawnClass = ASpectatorPawn::StaticClass();
	PlayerControllerClass = ATHRMainMenuPlayerController::StaticClass();
	HUDClass = ATHRMainMenuHUD::StaticClass();
}
