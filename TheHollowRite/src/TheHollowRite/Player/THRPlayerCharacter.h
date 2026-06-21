/// The Hollow Rite - プレイヤーキャラクター（三人称・SFボスアクション）

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Character.h"
#include "THRPlayerCharacter.generated.h"

class USpringArmComponent;
class UCameraComponent;
class UInputAction;
class UInputMappingContext;
class UTHRHealthComponent;
class UTHRMeleeHitboxComponent;
class UAnimMontage;
class ATHRProjectile;
struct FInputActionValue;

/// プレイヤーの装備武器
UENUM(BlueprintType)
enum class ETHRWeaponType : uint8
{
	Saber,   /// ビームセイバー（近接）
	Blaster  /// ブラスター（遠距離・投射体）
};

/**
 * SFボスアクションの三人称プレイヤーキャラクター。
 * カメラ相対の移動・視点操作、無敵フレーム付きの回避、近接攻撃をサポートする。
 */
UCLASS()
class ATHRPlayerCharacter final : public ACharacter
{
	GENERATED_BODY()

public:
	ATHRPlayerCharacter();

	/// 残り回復薬の数を返す（HUD表示用）
	UFUNCTION(BlueprintPure, Category="THR|Player|Potion")
	int32 GetPotionsRemaining() const { return PotionsRemaining; }

	/// 現在装備中の武器（HUD表示用）
	UFUNCTION(BlueprintPure, Category="THR|Player|Weapon")
	ETHRWeaponType GetCurrentWeapon() const { return CurrentWeapon; }

	/// 武器スワップ中か（HUD表示用）
	UFUNCTION(BlueprintPure, Category="THR|Player|Weapon")
	bool IsSwapping() const { return bSwapping; }

	/// スワップ先の武器（スワップ中のHUD表示用）
	UFUNCTION(BlueprintPure, Category="THR|Player|Weapon")
	ETHRWeaponType GetPendingWeapon() const { return PendingWeapon; }

protected:
	virtual void BeginPlay() override;
	virtual void SetupPlayerInputComponent(UInputComponent* PlayerInputComponent) override;

	/* ── コンポーネント ─────────────────────────────────────── */

	/// カメラブーム（三人称の追従アーム）
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Components")
	TObjectPtr<USpringArmComponent> CameraBoom;

	/// 追従カメラ
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Components")
	TObjectPtr<UCameraComponent> FollowCamera;

	/// 体力コンポーネント
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Components")
	TObjectPtr<UTHRHealthComponent> HealthComponent;

	/// 近接攻撃ヒットボックス（モンタージュ攻撃への移行用）
	UPROPERTY(VisibleAnywhere, BlueprintReadOnly, Category="THR|Components")
	TObjectPtr<UTHRMeleeHitboxComponent> MeleeHitbox;

	/// 死亡からレベル再開までの待ち時間（秒）
	UPROPERTY(EditAnywhere, Category="THR|Player|Death")
	float RestartDelay = 2.0f;

	/* ── 入力（コンストラクタでデフォルトロード） ──────────── */

	UPROPERTY(EditAnywhere, Category="THR|Input")
	TObjectPtr<UInputMappingContext> DefaultMappingContext;

	UPROPERTY(EditAnywhere, Category="THR|Input")
	TObjectPtr<UInputAction> MoveAction;

	UPROPERTY(EditAnywhere, Category="THR|Input")
	TObjectPtr<UInputAction> LookAction;

	UPROPERTY(EditAnywhere, Category="THR|Input")
	TObjectPtr<UInputAction> DodgeAction;

	UPROPERTY(EditAnywhere, Category="THR|Input")
	TObjectPtr<UInputAction> AttackAction;

	/* ── 移動 ──────────────────────────────────────────────── */

	/// 移動速度（cm/秒）
	UPROPERTY(EditAnywhere, Category="THR|Player|Movement")
	float MoveSpeed = 500.0f;

	/* ── 回避 ──────────────────────────────────────────────── */

	/// 回避の移動インパルス（cm/秒）
	UPROPERTY(EditAnywhere, Category="THR|Player|Dodge")
	float DodgeImpulse = 900.0f;

	/// 回避の継続時間（秒）。この間は無敵フレーム。
	UPROPERTY(EditAnywhere, Category="THR|Player|Dodge")
	float DodgeDuration = 0.35f;

	/// 回避のクールダウン（秒）
	UPROPERTY(EditAnywhere, Category="THR|Player|Dodge")
	float DodgeCooldown = 0.6f;

	/* ── ビームセイバー（近接、武器1） ─────────────────────── */

	/// 近接攻撃モンタージュ（判定はモンタージュ内の THR Melee Hit 通知が行う）
	UPROPERTY(EditAnywhere, Category="THR|Player|Saber")
	TObjectPtr<UAnimMontage> AttackMontage;

	/// 攻撃判定の前方距離（cm）※モンタージュ未設定時のフォールバック用
	UPROPERTY(EditAnywhere, Category="THR|Player|Saber")
	float AttackRange = 200.0f;

	/// 攻撃判定の半径（cm）※フォールバック用
	UPROPERTY(EditAnywhere, Category="THR|Player|Saber")
	float AttackRadius = 80.0f;

	/// 近接ダメージ ※フォールバック用
	UPROPERTY(EditAnywhere, Category="THR|Player|Saber")
	float AttackDamage = 25.0f;

	/// 近接クールダウン（秒）。モンタージュ使用時は再生時間との長い方を採用
	UPROPERTY(EditAnywhere, Category="THR|Player|Saber")
	float AttackCooldown = 0.5f;

	/* ── ブラスター（遠距離、武器2） ───────────────────────── */

	/// 発射する投射体クラス
	UPROPERTY(EditAnywhere, Category="THR|Player|Blaster")
	TSubclassOf<ATHRProjectile> ProjectileClass;

	/// 投射体のダメージ
	UPROPERTY(EditAnywhere, Category="THR|Player|Blaster")
	float BlasterDamage = 12.0f;

	/// ブラスターのクールダウン（秒）
	UPROPERTY(EditAnywhere, Category="THR|Player|Blaster")
	float BlasterCooldown = 0.4f;

	/// 銃口の前方オフセット（cm）
	UPROPERTY(EditAnywhere, Category="THR|Player|Blaster")
	float MuzzleForwardOffset = 80.0f;

	/// 銃口の高さオフセット（cm）
	UPROPERTY(EditAnywhere, Category="THR|Player|Blaster")
	float MuzzleHeight = 50.0f;

	/* ── 武器スワップ ──────────────────────────────────────── */

	/// 武器切替に要する時間（秒）。この間は攻撃不可。
	UPROPERTY(EditAnywhere, Category="THR|Player|Weapon")
	float SwapTime = 0.4f;

	/* ── 回復薬 ────────────────────────────────────────────── */

	/// 回復薬の所持上限（戦闘開始時に全回復）
	UPROPERTY(EditAnywhere, Category="THR|Player|Potion")
	int32 MaxPotions = 3;

	/// 回復薬1個の回復量
	UPROPERTY(EditAnywhere, Category="THR|Player|Potion")
	float PotionHealAmount = 35.0f;

private:
	/* 入力ハンドラ */
	void HandleMove(const FInputActionValue& Value);
	void HandleLook(const FInputActionValue& Value);
	void HandleDodge();
	void HandleAttack();

	/* 武器別攻撃 */
	void PerformSaberAttack();
	void PerformBlasterAttack();

	/* 武器選択（1=セイバー / 2=ブラスター） */
	void SelectSaber();
	void SelectBlaster();

	/* デバッグ：ボスをフェーズ2/3へ強制遷移（F3/F4） */
	void DebugForcePhase2();
	void DebugForcePhase3();
	void BeginSwap(ETHRWeaponType NewWeapon);
	void FinishSwap();

	/// 死亡時のコールバック（入力遮断 → レベル再開）
	UFUNCTION()
	void HandleDeath();

	/* 回復薬を使用する（Rキー） */
	void HandlePotion();

	/* 残り回復薬 */
	int32 PotionsRemaining = 0;

	/* 武器状態 */
	ETHRWeaponType CurrentWeapon = ETHRWeaponType::Saber;
	ETHRWeaponType PendingWeapon = ETHRWeaponType::Saber;
	bool bSwapping = false;
	float LastSaberTime = -100.0f;
	float LastBlasterTime = -100.0f;
	FTimerHandle SwapTimerHandle;

	/* 回避終了時のコールバック */
	void EndDodge();

	/* 回避方向を計算する（移動入力があればその方向、なければ正面） */
	FVector GetDodgeDirection() const;

	/* 実行時フラグ */
	bool bIsDodging = false;
	bool bCanDodge = true;

	/* 最後の移動入力（回避方向の決定に使用） */
	FVector2D LastMoveInput = FVector2D::ZeroVector;

	/* タイマーハンドル */
	FTimerHandle DodgeTimerHandle;
	FTimerHandle DodgeCooldownTimerHandle;
};
