/// The Hollow Rite - 範囲攻撃（AOE）データ定義

#pragma once

#include "CoreMinimal.h"
#include "THRAOETypes.generated.h"

/// 範囲攻撃のテレグラフ形状
UENUM(BlueprintType)
enum class ETHRAOEShape : uint8
{
	Circle,
	Cone,
	Donut,
	Line,
	Cross
};

/// 範囲攻撃の危険度（テレグラフ色に対応）
UENUM(BlueprintType)
enum class ETHRAOESeverity : uint8
{
	Normal,  /// 通常（シアン）
	Danger,  /// 大ダメージ（赤）
	Lethal   /// 即死級（金）
};

/// 範囲攻撃の基準位置
UENUM(BlueprintType)
enum class ETHRAOEAnchor : uint8
{
	Self,    /// ボス中心
	Target,  /// 詠唱開始時のプレイヤー位置（スナップショット）
	Offset   /// ボスのローカルオフセット位置
};

/**
 * 範囲攻撃1件分の定義。
 * 形状ごとに使用する寸法フィールドが異なる：
 * - Circle: OuterRadius
 * - Donut:  InnerRadius 〜 OuterRadius
 * - Cone:   OuterRadius + AngleDeg（前方扇形）
 * - Line:   Length × Width（原点から前方へ）
 * - Cross:  Length × Width（中心から十字）
 */
USTRUCT(BlueprintType)
struct FTHRAOEDef
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE")
	ETHRAOEShape Shape = ETHRAOEShape::Circle;

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE")
	ETHRAOESeverity Severity = ETHRAOESeverity::Normal;

	/// 外径（Circle/Donut/Cone の半径、cm）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE", meta=(ClampMin="0"))
	float OuterRadius = 500.0f;

	/// 内径（Donut の安全圏半径、cm）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE", meta=(ClampMin="0"))
	float InnerRadius = 0.0f;

	/// 扇形の全角（Cone、度）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE", meta=(ClampMin="1", ClampMax="360"))
	float AngleDeg = 90.0f;

	/// 長さ（Line/Cross、cm）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE", meta=(ClampMin="0"))
	float Length = 800.0f;

	/// 幅（Line/Cross、cm）
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE", meta=(ClampMin="0"))
	float Width = 200.0f;

	/// テレグラフが満ちるまでの時間（秒）。満了時に発動・判定
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE", meta=(ClampMin="0.1"))
	float TelegraphTime = 2.5f;

	/// 命中時のダメージ
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE", meta=(ClampMin="0"))
	float Damage = 20.0f;
};

/**
 * 攻撃パターンに紐づく範囲攻撃のスポーン指定。
 * 1つの攻撃が複数の範囲攻撃を生成できる（同時多重円など）。
 */
USTRUCT(BlueprintType)
struct FTHRAOESpawnDef
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE")
	FTHRAOEDef AOE;

	/// スポーン基準位置
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE")
	ETHRAOEAnchor Anchor = ETHRAOEAnchor::Self;

	/// Anchor=Offset 時のボスローカルオフセット
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE")
	FVector Offset = FVector::ZeroVector;

	/// 攻撃開始からスポーンまでの遅延（秒）。0 = 即時
	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category="THR|AOE", meta=(ClampMin="0"))
	float DelayFromAttackStart = 0.0f;
};
