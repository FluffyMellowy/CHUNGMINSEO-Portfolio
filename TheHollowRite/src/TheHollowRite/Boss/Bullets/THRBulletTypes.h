/// The Hollow Rite - 弾幕パターン定義

#pragma once

#include "CoreMinimal.h"
#include "THRBulletTypes.generated.h"

/// 弾幕の発射パターン種別
UENUM(BlueprintType)
enum class ETHRBulletEmitter : uint8
{
	Radial,  /// 放射状（円周に均等配置）
	Spiral,  /// 螺旋（波ごとに角度をずらす）
	Wave,    /// 波状（正面扇形を往復）
	Aimed    /// 自機狙い（プレイヤー方向へ扇）
};

/**
 * 弾幕パターン1件分の定義。
 * 1つの「波（wave）」で BulletsPerWave 発を撃ち、それを WaveInterval 間隔で WaveCount 回繰り返す。
 * エミッタ種別ごとに角度の決め方が変わる。
 */
USTRUCT(BlueprintType)
struct FTHRBulletPatternDef
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Bullet")
	ETHRBulletEmitter Emitter = ETHRBulletEmitter::Radial;

	/// 1波あたりの弾数
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Bullet", meta=(ClampMin="1"))
	int32 BulletsPerWave = 16;

	/// 波の回数
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Bullet", meta=(ClampMin="1"))
	int32 WaveCount = 1;

	/// 波と波の間隔（秒）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Bullet", meta=(ClampMin="0"))
	float WaveInterval = 0.3f;

	/// 弾速（cm/秒）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Bullet", meta=(ClampMin="0"))
	float BulletSpeed = 600.0f;

	/// 1発のダメージ
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Bullet", meta=(ClampMin="0"))
	float Damage = 8.0f;

	/// 散布する弧の角度（度）。Radial で 360 なら全周。Aimed/Wave では扇の広がり。
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Bullet", meta=(ClampMin="0", ClampMax="360"))
	float ArcDeg = 360.0f;

	/// Spiral：波ごとに加える角度オフセット（度）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Bullet")
	float SpinDegPerWave = 13.0f;

	/// 弾の半径（見た目＋当たり判定、cm）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Bullet", meta=(ClampMin="1"))
	float BulletRadius = 24.0f;

	/// 弾の寿命（秒）。アリーナ外へ出るまで生存させる目安。
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Bullet", meta=(ClampMin="0.1"))
	float BulletLifetime = 5.0f;

	/// 攻撃開始から発射開始までの遅延（秒）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|Bullet", meta=(ClampMin="0"))
	float DelayFromAttackStart = 0.4f;
};
