/// The Hollow Rite - ゲームモード

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/GameModeBase.h"
#include "THRGameMode.generated.h"

/**
 * The Hollow Rite のゲームモード。
 * THRPlayerCharacter をデフォルトポーンとして設定する。
 */
UCLASS()
class ATHRGameMode final : public AGameModeBase
{
	GENERATED_BODY()

public:
	ATHRGameMode();

	virtual void BeginPlay() override;
};
