using System;
using System.Collections.Generic;

namespace TheTrueBlackSilence
{
    // =========================================================================
    // PASSIVE 1: SUSTAINABILITY TRAINING
    // =========================================================================
    public class PassiveAbility_SustainabilityTraining : PassiveAbilityBase
    {
        private int turnCounter = 0;

        public override void OnRoundStart()
        {
            turnCounter++;
            if (turnCounter > 4) turnCounter = 4;
            
            int x = turnCounter;
            int lightToGain = x / 2; // C# integer division automatically acts as Math.Floor()

            this.owner.RecoverHP(x);
            this.owner.cardSlotDetail.RecoverPlayPoint(lightToGain);
        }

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            int x = turnCounter;
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = x });
        }
    }

    // =========================================================================
    // PASSIVE 2: I AM TOO FAST FOR YOU
    // =========================================================================
    public class PassiveAbility_TooFastForYou : PassiveAbilityBase
    {
        public override void OnSelectCardTeam(BattlePlayingCardDataInUnitModel card)
        {
            if (card == null || card.target == null) return;

            foreach (var enemyCard in card.target.cardSlotDetail.cardList)
            {
                if (enemyCard != null && enemyCard.target == this.owner && enemyCard.isOneSided)
                {
                    BattleDiceBehavior evadeDice = new BattleDiceBehavior();
                    evadeDice.behaviourInCard = new DiceBehaviour
                    {
                        Detail = BehaviourDetail.Evasion,
                        Min = 9,
                        Max = 20
                    };
                    card.AddDice(evadeDice);
                }
            }
        }

        public override void OnLoseClash(BattleDiceBehavior behavior)
        {
            if (behavior.card != null)
            {
                int originalDamage = behavior.card.GetDamageValue();
                int reducedDamage = originalDamage / 5; // Reduces incoming failed clash damage by 80%
                behavior.card.AddDamageValueModifier(-reducedDamage);
            }
        }
    }

    // =========================================================================
    // PASSIVE 3: FINAL STAND
    // =========================================================================
    public class PassiveAbility_FinalStand : PassiveAbilityBase
    {
        private bool triggeredSoloBuffs = false;
        private bool usedReviveSwitch = false;

        public override void OnRoundStart()
        {
            List<BattleUnitModel> aliveAllies = BattleObjectManager.instance.GetAliveList(this.owner.faction);
            if (aliveAllies.Count == 1 && aliveAllies[0] == this.owner && !triggeredSoloBuffs)
            {
                triggeredSoloBuffs = true;
                int deadCount = BattleObjectManager.instance.GetDeadList(this.owner.faction).Count;

                int currentDiceCount = this.owner.speedDiceCount;
                int newDiceCount = currentDiceCount + deadCount;
                if (newDiceCount > 9) newDiceCount = 9;

                this.owner.SetSpeedDiceCount(newDiceCount);
                this.owner.cardSlotDetail.SetMaxPlayPoint(this.owner.cardSlotDetail.GetMaxPlayPoint() + deadCount);

                this.owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Quickness, 2, this.owner);
                this.owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, 2, this.owner);
            }

            if (triggeredSoloBuffs)
            {
                this.owner.allyCardDetail.DrawCard(this.owner.speedDiceCount);
            }
        }

        public override void OnDamageAbsolute(int damage, DamageType type)
        {
            TriggerEmergencySurvival();
        }

        public override void OnBreakAbsolute(int breakDamage)
        {
            TriggerEmergencySurvival();
        }

        private void TriggerEmergencySurvival()
        {
            if ((this.owner.hp <= 1 || this.owner.IsBreak()) && !usedReviveSwitch)
            {
                usedReviveSwitch = true;
                this.owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, 99, this.owner);
                this.owner.RecoverHP(this.owner.MaxBreak);

                if (this.owner.IsBreak())
                {
                    this.owner.breakDetail.RecoverBreak(this.owner.MaxBreak);
                }
            }
        }
    }
}
