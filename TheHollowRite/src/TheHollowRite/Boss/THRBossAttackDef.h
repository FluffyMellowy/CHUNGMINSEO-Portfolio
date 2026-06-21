/// The Hollow Rite - ボス攻撃パターン定義

#pragma once

#include "CoreMinimal.h"
#include "AOE/THRAOETypes.h"
#include "Bullets/THRBulletTypes.h"
#include "THRBossAttackDef.generated.h"

class UAnimMontage;

/**
 * ボスの攻撃パターン1件分の定義。
 * ボスの AttackTable に行として並べ、距離・クールダウン・重みに基づいて選択される。
 * ダメージ・判定リーチ・半径はモンタージュ側のアニメ通知（THR Melee Hit）が持つ。
 */
USTRUCT(BlueprintType)
struct FTHRBossAttackDef
{
	GENERATED_BODY()

	/// 識別名（クールダウン管理・デバッグ表示に使用）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|Attack")
	FName AttackName = NAME_None;

	/// 再生するモンタージュ
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|Attack")
	TObjectPtr<UAnimMontage> Montage = nullptr;

	/// 使用可能な最小距離（cm）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|Attack", meta=(ClampMin="0"))
	float MinRange = 0.0f;

	/// 使用可能な最大距離（cm）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|Attack", meta=(ClampMin="0"))
	float MaxRange = 250.0f;

	/// 再使用までのクールダウン（秒）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|Attack", meta=(ClampMin="0"))
	float Cooldown = 3.0f;

	/// 加重ランダム選択の重み
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|Attack", meta=(ClampMin="0"))
	float Weight = 1.0f;

	/// 使用可能になる最小フェーズ（1〜3。フェーズが進むと解禁されるパターン用）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|Attack", meta=(ClampMin="1", ClampMax="3"))
	int32 MinPhase = 1;

	/// 攻撃開始時にターゲット方向へ放つ突進インパルス（0 = 突進なし）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|Attack", meta=(ClampMin="0"))
	float LungeImpulse = 0.0f;

	/// この攻撃が生成する範囲攻撃（チャネルB）。空なら直接打のみ。
	/// 直接打と併記すれば複合パターン（A+B）になる。
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|Attack")
	TArray<FTHRAOESpawnDef> AOESpawns;

	/// この攻撃が発射する弾幕パターン（チャネルC）。空なら弾幕なし。
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Boss|Attack")
	TArray<FTHRBulletPatternDef> BulletPatterns;
};
