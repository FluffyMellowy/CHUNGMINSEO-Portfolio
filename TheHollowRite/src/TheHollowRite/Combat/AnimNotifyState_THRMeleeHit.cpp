/// The Hollow Rite - 近接攻撃ヒット用アニメーション通知ステート

#include "AnimNotifyState_THRMeleeHit.h"
#include "THRMeleeHitboxComponent.h"
#include "Components/SkeletalMeshComponent.h"

void UAnimNotifyState_THRMeleeHit::NotifyBegin(USkeletalMeshComponent* MeshComp,
	UAnimSequenceBase* Animation, float TotalDuration, const FAnimNotifyEventReference& EventReference)
{
	Super::NotifyBegin(MeshComp, Animation, TotalDuration, EventReference);

	if (MeshComp && MeshComp->GetOwner())
	{
		if (UTHRMeleeHitboxComponent* Hitbox =
			MeshComp->GetOwner()->FindComponentByClass<UTHRMeleeHitboxComponent>())
		{
			Hitbox->OpenWindow(Damage, Range, Radius);
		}
	}
}

void UAnimNotifyState_THRMeleeHit::NotifyEnd(USkeletalMeshComponent* MeshComp,
	UAnimSequenceBase* Animation, const FAnimNotifyEventReference& EventReference)
{
	Super::NotifyEnd(MeshComp, Animation, EventReference);

	if (MeshComp && MeshComp->GetOwner())
	{
		if (UTHRMeleeHitboxComponent* Hitbox =
			MeshComp->GetOwner()->FindComponentByClass<UTHRMeleeHitboxComponent>())
		{
			Hitbox->CloseWindow();
		}
	}
}
