/// The Hollow Rite - メインメニュー用ゲームモード

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "THRMainMenuGameMode.generated.h"

/**
 * メインメニューレベル専用のゲームモード。
 * 操作ポーンは不要なため SpectatorPawn を使い、
 * メニュー描画（THRMainMenuHUD）と入力（THRMainMenuPlayerController）を割り当てる。
 */
UCLASS()
class ATHRMainMenuGameMode final : public AGameModeBase
{
	GENERATED_BODY()

public:
	ATHRMainMenuGameMode();
};
