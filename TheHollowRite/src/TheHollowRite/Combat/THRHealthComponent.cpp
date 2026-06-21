/// The Hollow Rite - 体力コンポーネント（プレイヤー・ボス共用）

#include "THRHealthComponent.h"
#include "Engine/Engine.h"
#include "TheHollowRite.h"

UTHRHealthComponent::UTHRHealthComponent()
{
	/* 体力管理に毎フレーム更新は不要 */
	PrimaryComponentTick.bCanEverTick = false;
}

void UTHRHealthComponent::BeginPlay()
{
	Super::BeginPlay();
	CurrentHealth = MaxHealth;
}

void UTHRHealthComponent::ApplyDamage(float Amount)
{
	if (bIsDead || bIsInvulnerable || Amount <= 0.0f)
	{
		return;
	}

	CurrentHealth = FMath::Clamp(CurrentHealth - Amount, 0.0f, MaxHealth);
	OnHealthChanged.Broadcast(CurrentHealth, MaxHealth);

	/* M1の動作確認用：被ダメージ対象のHPを画面に表示 */
	if (GEngine)
	{
		const FString Msg = FString::Printf(TEXT("%s HP: %.0f / %.0f"),
			*GetNameSafe(GetOwner()), CurrentHealth, MaxHealth);
		GEngine->AddOnScreenDebugMessage(1, 2.0f, FColor::Cyan, Msg);
	}

	if (CurrentHealth <= 0.0f)
	{
		bIsDead = true;
		OnDeath.Broadcast();
	}
}

void UTHRHealthComponent::Heal(float Amount)
{
	if (bIsDead || Amount <= 0.0f)
	{
		return;
	}

	CurrentHealth = FMath::Clamp(CurrentHealth + Amount, 0.0f, MaxHealth);
	OnHealthChanged.Broadcast(CurrentHealth, MaxHealth);
}

void UTHRHealthComponent::SetInvulnerable(bool bNewInvulnerable)
{
	bIsInvulnerable = bNewInvulnerable;
}

void UTHRHealthComponent::SetMaxHealth(float NewMaxHealth, bool bResetCurrent)
{
	MaxHealth = FMath::Max(1.0f, NewMaxHealth);
	if (bResetCurrent)
	{
		CurrentHealth = MaxHealth;
	}
	else
	{
		CurrentHealth = FMath::Min(CurrentHealth, MaxHealth);
	}
	OnHealthChanged.Broadcast(CurrentHealth, MaxHealth);
}
