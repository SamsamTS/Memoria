using System;
using System.Linq;
using System.Collections.Generic;
using Assets.Sources.Scripts.UI.Common;
using FF9;
using Memoria.Assets;
using Memoria.Data;
using Memoria.Database;
using Memoria.Prime;
using Memoria.Prime.Text;
using UnityEngine;
using Object = System.Object;

namespace Memoria
{
    public class BattleUnit
    {
        public CalcFlag Flags;
        public Int32 HpDamage;
        public Int32 MpDamage;

        public BTL_DATA Data;

        public BattleUnit(BTL_DATA data)
        {
            Data = data;
        }

        public static implicit operator BTL_DATA(BattleUnit unit) => unit.Data;

        public UInt16 Id => Data.btl_id;
        public Boolean IsPlayer => Data.bi.player != 0;
        public Boolean IsNonMorphedPlayer => Data.bi.player != 0 && !Data.is_monster_transform;
        public Boolean IsTargetable => Data.bi.target != 0;
        public Boolean IsSlave => Data.bi.slave != 0;
        public Boolean IsDisappear => Data.bi.disappear != 0;
        public Boolean IsOutOfReach
        {
            get => Data.out_of_reach;
            set => Data.out_of_reach = value;
        }
        public Boolean CanMove => Data.bi.atb != 0;
        public CharacterId PlayerIndex => IsPlayer ? (CharacterId)Data.bi.slot_no : CharacterId.NONE;

        public Byte Level
        {
            get => Data.level;
            set => Data.level = value;
        }
        public Byte Position => Data.bi.line_no;

        public Byte Row
        {
            get => Data.bi.row;
            set
            {
                if (value != Data.bi.row)
                    btl_para.SwitchPlayerRow(Data, false);
            }
        }
        public Boolean IsCovering => Data.bi.cover != 0;

        public Boolean IsDodged
        {
            get => Data.bi.dodge != 0;
            set => Data.bi.dodge = (Byte)(value ? 1 : 0);
        }

        public UInt32 MaximumHp
        {
            get => btl_para.GetLogicalHP(Data, true);
            set => btl_para.SetLogicalHP(Data, value, true);
        }
        public UInt32 CurrentHp
        {
            get => btl_para.GetLogicalHP(Data, false);
            set => btl_para.SetLogicalHP(Data, value, false);
        }

        public UInt32 MaximumMp
        {
            get => Data.max.mp;
            set => Data.max.mp = value;
        }
        public UInt32 CurrentMp
        {
            get => Data.cur.mp;
            set => Data.cur.mp = value;
        }

        public Int16 MaximumAtb => Data.max.at;
        public Int16 CurrentAtb
        {
            get => Data.cur.at;
            set => Data.cur.at = value;
        }

        public Int32 PhysicalDefence
        {
            get => Data.defence.PhysicalDefence;
            set => Data.defence.PhysicalDefence = value;
        }

        public Int32 PhysicalEvade
        {
            get => Data.defence.PhysicalEvade;
            set => Data.defence.PhysicalEvade = value;
        }

        public Int32 MagicDefence
        {
            get => Data.defence.MagicalDefence;
            set => Data.defence.MagicalDefence = value;
        }

        public Int32 MagicEvade
        {
            get => Data.defence.MagicalEvade;
            set => Data.defence.MagicalEvade = value;
        }

        public Byte Strength
        {
            get => Data.elem.str;
            set => Data.elem.str = value;
        }

        public Byte Magic
        {
            get => Data.elem.mgc;
            set => Data.elem.mgc = value;
        }

        public Byte Dexterity
        {
            get => Data.elem.dex;
            set
            {
                if (Data.elem.dex == value)
                    return;
                Data.elem.dex = value;
                Int16 newMaxATB = btl_para.GetMaxATB(this);
                if (Data.cur.at >= Data.max.at)
                {
                    Data.max.at = newMaxATB;
                    Data.cur.at = newMaxATB;
                }
                else
                {
                    Single atbFill = (Single)Data.cur.at / Data.max.at;
                    Data.max.at = newMaxATB;
                    Data.cur.at = (Int16)Math.Round(atbFill * newMaxATB);
                }
            }
        }

        public Byte Will
        {
            get => Data.elem.wpr;
            set => Data.elem.wpr = value;
        }

        public UInt32 MaxDamageLimit
        {
            get => Data.maxDamageLimit;
            set => Data.maxDamageLimit = value;
        }

        public UInt32 MaxMpDamageLimit
        {
            get => Data.maxMpDamageLimit;
            set => Data.maxMpDamageLimit = value;
        }

        public Color UIColorHP
        {
            get => Data.uiColorHP;
            set => Data.uiColorHP = value;
        }
        public Color UIColorMP
        {
            get => Data.uiColorMP;
            set => Data.uiColorMP = value;
        }
        public String UILabelHP
        {
            get => Data.uiLabelHP;
            set => Data.uiLabelHP = value;
        }
        public String UILabelMP
        {
            get => Data.uiLabelMP;
            set => Data.uiLabelMP = value;
        }
        public String UISpriteATB
        {
            get => Data.uiSpriteATB;
            set => Data.uiSpriteATB = value;
        }

        public Boolean HasTrance => Data.bi.t_gauge != 0;
        public Boolean InTrance => (CurrentStatus & BattleStatus.Trance) != 0;
        public Byte Trance
        {
            get => Data.trance;
            set => Data.trance = value;
        }

        public Int32 Fig
        {
            get => Data.fig.hp;
            set => Data.fig.hp = value;
        }
        public Int32 MagicFig
        {
            get => Data.fig.mp;
            set => Data.fig.mp = value;
        }
        public UInt16 FigInfo
        {
            get => Data.fig.info;
            set => Data.fig.info = value;
        }

        public Int32 WeaponRate => Data.weapon != null ? Data.weapon.Ref.Rate : 0;
        public Int32 WeaponPower => Data.weapon != null ? Data.weapon.Ref.Power : 0;
        public EffectElement WeaponElement => (EffectElement)(Data.weapon != null ? Data.weapon.Ref.Elements : 0);
        public BattleStatus WeaponStatus => Data.weapon != null ? FF9StateSystem.Battle.FF9Battle.add_status[Data.weapon.StatusIndex].Value : 0;
        public Int32 GetWeaponPower(BattleCommand cmd)
        {
            if (IsMonsterTransform)
                return Data.monster_transform.attack[Data.bi.def_idle]?.Ref.Power ?? 0;
            if (Data.weapon == null)
                return 0;
            if (Configuration.Battle.CustomBattleFlagsMeaning == 1 && FF9StateSystem.Battle.FF9Battle.btl_scene.Info.ReverseAttack && cmd != null && (cmd.AbilityType & 0x8) != 0)
                return Math.Max(1, 60 - WeaponPower);
            return WeaponPower;
        }

        public PLAYER Player => FF9StateSystem.Common.FF9.GetPlayer(PlayerIndex);
        public CharacterSerialNumber SerialNumber => btl_util.getSerialNumber(Data);
        public CharacterCategory PlayerCategory => IsPlayer ? Player.Category : 0;
        public EnemyCategory Category => IsPlayer ? EnemyCategory.Humanoid : (EnemyCategory)btl_util.getEnemyTypePtr(Data).category;
        public WeaponCategory WeapCategory => Data.weapon != null ? Data.weapon.Category : 0;
        public BattleEnemy Enemy => new BattleEnemy(btl_util.getEnemyPtr(Data));
        public ENEMY_TYPE EnemyType => btl_util.getEnemyTypePtr(Data);
        public String Name => IsPlayer ? Player.Name : Enemy.Name;
        public String NameTag => IsPlayer ? Player.NameTag : Enemy.Name;

        public BattleStatus CurrentStatus
        {
            get => Data.stat.cur;
            //set => Data.stat.cur = value; // Use AlterStatus/RemoveStatus instead
        }

        public BattleStatus PermanentStatus
        {
            get => Data.stat.permanent;
            //set => Data.stat.permanent = value; // Use btl_stat.MakeStatusesPermanent instead
        }

        public BattleStatus ResistStatus
        {
            get => Data.stat.invalid;
            set
            {
                Data.stat.invalid = value;
                if (!IsPlayer)
                    Data.bi.t_gauge = (Byte)((value & BattleStatus.Trance) == 0 ? 1 : 0);
            }
        }

        public StatusModifier PartialResistStatus => Data.stat.partial_resist;
        public StatusModifier StatusDurationFactor => Data.stat.duration_factor;

        public Int32 GetCurrentStatusContiCnt(BattleStatusId statusId)
        {
            if (Data.stat.conti.TryGetValue(statusId, out Int32 conti))
                return conti;
            return 0;
        }

        public StatusScriptBase GetCurrentStatusEffectScript(BattleStatusId statusId)
        {
            if (Data.stat.effects.TryGetValue(statusId, out StatusScriptBase script))
                return script;
            return null;
        }

        public EffectElement BonusElement
        {
            get => (EffectElement)Data.p_up_attr;
            set => Data.p_up_attr = (Byte)value;
        }
        public EffectElement WeakElement
        {
            get => (EffectElement)Data.def_attr.weak;
            set => Data.def_attr.weak = (Byte)value;
        }
        public EffectElement GuardElement
        {
            get => (EffectElement)Data.def_attr.invalid;
            set => Data.def_attr.invalid = (Byte)value;
        }
        public EffectElement AbsorbElement
        {
            get => (EffectElement)Data.def_attr.absorb;
            set => Data.def_attr.absorb = (Byte)value;
        }
        public EffectElement HalfElement
        {
            get => (EffectElement)Data.def_attr.half;
            set => Data.def_attr.half = (Byte)value;
        }

        public Boolean IsLevitate => HasCategory(EnemyCategory.Flight) || IsUnderAnyStatus(BattleStatus.Float);
        public Boolean IsZombie => HasCategory(EnemyCategory.Undead) || IsUnderAnyStatus(BattleStatusConst.ZombieEffect);
        public Boolean HasLongRangeWeapon => HasCategory(WeaponCategory.LongRange);

        public BattleCommand ATBCommand => new BattleCommand(Data.cmd[0]);
        public BattleCommand CounterCommand => new BattleCommand(Data.cmd[1]);
        public BattleCommand PetrifyCommand => new BattleCommand(Data.cmd[2]);
        public Boolean KillCommand(BattleCommand cmd, Boolean resetATBifNeeded = true)
        {
            Boolean isMainCommand = btl_util.IsCommandDeclarable(cmd.Id);
            btl_cmd.KillCommand(cmd);
            if (!isMainCommand)
                return false;
            if (IsPlayer)
                UIManager.Battle.RemovePlayerFromAction(Id, true);
            Data.sel_mode = 0;
            if (resetATBifNeeded)
                CurrentAtb = 0;
            return true;
        }
        public Boolean KillStandardCommands(Boolean resetATBifNeeded = true)
        {
            Boolean killMainCommand = btl_cmd.KillStandardCommands(Data);
            if (!killMainCommand)
                return false;
            if (IsPlayer)
                UIManager.Battle.RemovePlayerFromAction(Id, true);
            Data.sel_mode = 0;
            if (resetATBifNeeded)
                CurrentAtb = 0;
            return true;
        }

        public RegularItem Weapon => btl_util.getWeaponNumber(Data);
        public RegularItem Head => IsPlayer ? FF9StateSystem.Common.FF9.GetPlayer(PlayerIndex).equip.Head : RegularItem.NoItem;
        public RegularItem Wrist => IsPlayer ? FF9StateSystem.Common.FF9.GetPlayer(PlayerIndex).equip.Wrist : RegularItem.NoItem;
        public RegularItem Armor => IsPlayer ? FF9StateSystem.Common.FF9.GetPlayer(PlayerIndex).equip.Armor : RegularItem.NoItem;
        public RegularItem Accessory => IsPlayer ? FF9StateSystem.Common.FF9.GetPlayer(PlayerIndex).equip.Accessory : RegularItem.NoItem;
        public Boolean IsHealingRod => IsPlayer && Weapon == RegularItem.HealingRod;

        public BattleUnit GetKiller()
        {
            return Data.killer_track != null ? new BattleUnit(Data.killer_track) : null;
        }

        public void AddDelayedModifier(BTL_DATA.DelayedModifier.IsDelayedDelegate delayDelegate, BTL_DATA.DelayedModifier.ApplyDelegate applyDelegate)
        {
            if (delayDelegate == null)
            {
                Data.delayedModifierList.Add(new BTL_DATA.DelayedModifier()
                {
                    isDelayed = btl => false,
                    apply = applyDelegate
                });
                return;
            }
            Data.delayedModifierList.Add(new BTL_DATA.DelayedModifier()
            {
                isDelayed = delayDelegate,
                apply = applyDelegate
            });
        }

        public Boolean IsPlayingMotion(BattlePlayerCharacter.PlayerMotionIndex motionIndex) => btl_mot.checkMotion(Data, motionIndex);
        public Boolean IsPlayingIdleMotion() => btl_mot.checkMotion(Data, Data.bi.def_idle);

        public UInt16 SummonCount
        {
            get => Data.summon_count;
            set => Data.summon_count = value;
        }
        public Int16 CriticalRateBonus
        {
            get => Data.critical_rate_deal_bonus;
            set => Data.critical_rate_deal_bonus = value;
        }
        public Int16 CriticalRateResistance
        {
            get => Data.critical_rate_receive_resistance;
            set => Data.critical_rate_receive_resistance = value;
        }

        public Boolean IsMonsterTransform => Data.is_monster_transform;
        public Boolean CanUseTheAttackCommand => !Data.is_monster_transform || Data.monster_transform.attack[Data.bi.def_idle] != null;

        public Vector3 CurrentPosition => Data.pos;
        public Vector3 DefaultPosition => Data.base_pos;
        public void ChangePositionCoordinate(Single position, Int32 coord, Boolean current = true, Boolean def = false, Boolean eventPos1 = false, Boolean eventPos2 = false)
        {
            if (current)
                Data.pos[coord] = position;
            if (def)
                Data.base_pos[coord] = position;
            if (eventPos1)
                Data.evt.posBattle[coord] = position;
            if (eventPos2)
                Data.evt.pos[coord] = position;
        }

        public Single CurrentOrientationAngle
        {
            get => Data.rot.eulerAngles.y;
            set
            {
                Vector3 eulerAngles = Data.rot.eulerAngles;
                Data.rot.eulerAngles = new Vector3(eulerAngles.x, value, eulerAngles.z);
            }
        }
        public Single DefaultOrientationAngle => Data.evt.rotBattle.eulerAngles.y;

        public Vector3 ModelStatusScale
        {
            get => Data.geoScaleStatus;
            set => Data.geoScaleStatus = value;
        }
        public Int32 ModelScaleX
        {
            get => Data.geo_scale_x;
            set => geo.geoScaleSetXYZ(Data, value, Data.geo_scale_y, Data.geo_scale_z, false);
        }
        public Int32 ModelScaleY
        {
            get => Data.geo_scale_y;
            set => geo.geoScaleSetXYZ(Data, Data.geo_scale_x, value, Data.geo_scale_z, false);
        }
        public Int32 ModelScaleZ
        {
            get => Data.geo_scale_z;
            set => geo.geoScaleSetXYZ(Data, Data.geo_scale_x, Data.geo_scale_y, value, false);
        }

        public void ScaleModel(Int32 size, Boolean setDefault = false) // 4096 is the default size
        {
            if (setDefault)
                Data.geo_scale_default = size;
            geo.geoScaleSet(Data, size, true);
        }

        public Boolean IsUnderStatus(BattleStatus status)
        {
            return (CurrentStatus & status) != 0;
        }

        public Boolean IsUnderStatus(BattleStatusId statusId)
        {
            return (CurrentStatus & statusId.ToBattleStatus()) != 0;
        }

        public Boolean IsUnderPermanentStatus(BattleStatus status)
        {
            return (PermanentStatus & status) != 0;
        }

        public Boolean IsUnderPermanentStatus(BattleStatusId statusId)
        {
            return (PermanentStatus & statusId.ToBattleStatus()) != 0;
        }

        public Boolean IsUnderAnyStatus(BattleStatus status)
        {
            // Permanent statuses are also current, at least that's the target behaviour
            //return ((CurrentStatus | PermanentStatus) & status) != 0;
            return (CurrentStatus & status) != 0;
        }

        public Boolean IsUnderAnyStatus(BattleStatusId statusId)
        {
            return (CurrentStatus & statusId.ToBattleStatus()) != 0;
        }

        public Boolean HasCategory(CharacterCategory category)
        {
            return (PlayerCategory & category) != 0;
        }

        public Boolean HasCategory(EnemyCategory category)
        {
            return btl_util.CheckEnemyCategory(Data, (Byte)category);
        }

        public Boolean HasCategory(WeaponCategory category)
        {
            if (Data.weapon == null)
                return false;

            return (Data.weapon.Category & category) != 0;
        }

        public Boolean HasSupportAbility(SupportAbility1 ability)
        {
            return (Data.sa[0] & (UInt32)ability) != 0;
        }

        public Boolean HasSupportAbility(SupportAbility2 ability)
        {
            return (Data.sa[1] & (UInt32)ability) != 0;
        }

        public Boolean HasSupportAbilityByIndex(SupportAbility saIndex)
        {
            return Data.saExtended.Contains(saIndex);
            //Int32 index = (Int32)saIndex;
            //if (abilId < 0) return false;
            //if (abilId < 32) return HasSupportAbility((SupportAbility1)(1u << index));
            //if (abilId < 64) return HasSupportAbility((SupportAbility2)(1u << index));
            //return Data.saExtended.Contains(saIndex);
        }

        public Boolean TryRemoveStatuses(BattleStatus status)
        {
            return btl_stat.RemoveStatuses(this, status) == 2U;
        }

        public void RemoveStatus(BattleStatus status)
        {
            btl_stat.RemoveStatuses(this, status);
        }

        public void RemoveStatus(BattleStatusId statusId)
        {
            btl_stat.RemoveStatus(this, statusId);
        }

        public void AlterStatus(BattleStatus status, BattleUnit inflicter = null)
        {
            btl_stat.AlterStatuses(this, status, inflicter);
        }

        public void AlterStatus(BattleStatusId status, BattleUnit inflicter = null)
        {
            btl_stat.AlterStatus(this, status, inflicter);
        }

        public void Kill(BattleUnit killer)
        {
            Kill(killer?.Data);
        }
        public void Kill(BTL_DATA killer = null)
        {
            CurrentHp = 0; // Use CurrentHp there to prevent killing for real the enemies that should never die (with the 10,000 HP threshold system)
            if (Data.cur.hp > 0) // Also, let the script handle the animations and sounds in that case
                return;

            Data.killer_track = killer;
            Data.bi.death_f = 1;
            if (!IsPlayer && !Enemy.AttackOnDeath)
            {
                btl_util.SetEnemyDieSound(Data, btl_util.getEnemyTypePtr(Data).die_snd_no);
                Data.die_seq = 3;
            }

            //if (!btl_mot.checkMotion(Data, Data.bi.def_idle) && (Data.bi.player == 0 || !btl_mot.checkMotion(Data, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD)) && !btl_util.IsBtlUsingCommand(Data))
            //{
            //    btl_mot.setMotion(Data, Data.bi.def_idle);
            //    Data.evt.animFrame = 0;
            //}
        }

        public void Remove(Boolean makeDisappear = true)
        {
            battle.btl_bonus.member_flag &= (Byte)~(1 << Data.bi.line_no);
            btl_cmd.ClearSysPhantom(Data);
            btl_cmd.KillAllCommands(Data);
            btl_sys.SavePlayerData(Data, true);
            btl_sys.DelCharacter(Data);
            if (makeDisappear)
                Data.SetDisappear(true, 5);
            btl_sys.CheckBattlePhase(Data); // Prevent a softlock when there are no more players present in combat.
            // The two following lines have been switched for fixing an UI bug (ATB bar glowing, etc... when an ally is snorted)
            // It seems to fix the bug without introducing another one (the HP/MP figures update strangely but that's because of how the UI cells are managed)
            UIManager.Battle.RemovePlayerFromAction(Data.btl_id, true);
            UIManager.Battle.DisplayParty(true);
        }

        public void FaceTheEnemy()
        {
            FaceAsUnit(this);
        }

        public void FaceAsUnit(BattleUnit unit)
        {
            Int32 angle = btl_mot.GetDirection(unit);
            Data.rot.eulerAngles = new Vector3(Data.rot.eulerAngles.x, angle, Data.rot.eulerAngles.z);
        }

        public void ChangeRowToDefault()
        {
            if (IsPlayer && Row != Player.info.row)
                btl_para.SwitchPlayerRow(Data);
        }

        public void ChangeRow()
        {
            btl_para.SwitchPlayerRow(Data);
        }

        public void Change(BattleUnit unit)
        {
            Data = unit.Data;
        }

        public void Libra(BattleHUD.LibraInformation infos = BattleHUD.LibraInformation.Default)
        {
            UIManager.Battle.SetBattleLibra(this, infos);
        }

        public void Detect(Boolean reverseOrder = true)
        {
            UIManager.Battle.SetBattlePeeping(this, reverseOrder);
        }

        public Int32 GetIndex()
        {
            Int32 index = 0;

            while (1 << index != Data.btl_id)
                ++index;

            return index;
        }

        public Boolean IsAbilityAvailable(BattleAbilityId abilId)
        {
            return UIManager.Battle.IsAbilityAvailable(this, ff9abil.GetAbilityIdFromActiveAbility(abilId));
        }

        public Boolean IsAbilityAvailable(SupportAbility abilId)
        {
            return UIManager.Battle.IsAbilityAvailable(this, ff9abil.GetAbilityIdFromSupportAbility(abilId));
        }

        public Boolean HasLearntAbility(BattleAbilityId abilId)
        {
            if (!IsPlayer)
                return false;
            return ff9abil.FF9Abil_IsMaster(Player, ff9abil.GetAbilityIdFromActiveAbility(abilId));
        }

        public Boolean HasLearntAbility(SupportAbility abilId)
        {
            if (!IsPlayer)
                return false;
            return ff9abil.FF9Abil_IsMaster(Player, ff9abil.GetAbilityIdFromSupportAbility(abilId));
        }

        public void DamageWithoutContext(Int32 damage, Int32 mpdamage = 0, Boolean hitAnimIfRelevant = true)
        {
            if (damage != 0)
            {
                Boolean motion = hitAnimIfRelevant && Data.bi.cover == 0 && !btl_stat.CheckStatus(Data, BattleStatusConst.NoDamageMotion);
                if (IsPlayer)
                    motion = motion && (btl_mot.checkMotion(Data, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_NORMAL) || btl_mot.checkMotion(Data, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_DYING) || btl_mot.checkMotion(Data, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD));
                else
                    motion = motion && btl_mot.checkMotion(Data, Data.bi.def_idle);
                if (damage > 0)
                    btl_para.SetDamage(this, damage, (Byte)(motion ? 1 : 0), requestFigureNow: true);
                else
                    btl_para.SetRecover(this, (UInt32)(-damage), requestFigureNow: true);
            }
            if (mpdamage != 0)
            {
                if (mpdamage > 0)
                {
                    CurrentMp = (UInt32)Math.Max(0, CurrentMp - mpdamage);
                    btl2d.Btl2dReqMP(Data, mpdamage, btl2d.DMG_COL_WHITE, (Byte)(damage != 0 ? 4 : 0));
                }
                else
                {
                    CurrentMp = (UInt32)Math.Min(MaximumMp, CurrentMp - mpdamage);
                    btl2d.Btl2dReqMP(Data, -mpdamage, btl2d.DMG_COL_GREEN, (Byte)(damage != 0 ? 4 : 0));
                }
            }
        }

        public void ChangeToMonster(String btlName, Int32 monsterIndex, BattleCommandId commandToReplace, BattleCommandId commandAsMonster, Boolean cancelOnDeath, Boolean updatePts, Boolean updateStat, Boolean updateDef, Boolean updateElement, List<BattleCommandId> disableCommands = null)
        {
            if (!IsPlayer) // In order to implement something similar for enemies, script has to be update for that enemy's entry, among other things
                return;
            BTL_SCENE scene = new BTL_SCENE();
            scene.ReadBattleScene(btlName);
            if (scene.header.TypCount <= 0)
                return;
            if (monsterIndex < 0)
                monsterIndex = Comn.random8() % scene.header.TypCount;
            if (monsterIndex >= scene.header.TypCount)
                return;
            SB2_MON_PARM monsterParam = scene.MonAddr[monsterIndex];
            Int32 i;
            btl_stat.RemoveStatuses(this, BattleStatusConst.RemoveOnMonsterTransform);
            if (updatePts)
            {
                Data.max.hp = monsterParam.MaxHP;
                Data.max.mp = monsterParam.MaxMP;
                Data.cur.hp = Math.Min(Data.cur.hp, Data.max.hp);
                Data.cur.mp = Math.Min(Data.cur.mp, Data.max.mp);
            }
            if (updateStat)
            {
                Strength = monsterParam.Element.Strength;
                Magic = monsterParam.Element.Magic;
                Dexterity = monsterParam.Element.Speed;
                Will = monsterParam.Element.Spirit;
            }
            if (updateDef)
            {
                Data.defence.PhysicalDefence = monsterParam.PhysicalDefence;
                Data.defence.PhysicalEvade = monsterParam.PhysicalEvade;
                Data.defence.MagicalDefence = monsterParam.MagicalDefence;
                Data.defence.MagicalEvade = monsterParam.MagicalEvade;
            }
            if (updateElement)
            {
                Data.def_attr.invalid = monsterParam.GuardElement;
                Data.def_attr.absorb = monsterParam.AbsorbElement;
                Data.def_attr.half = monsterParam.HalfElement;
                Data.def_attr.weak = monsterParam.WeakElement;
            }
            Data.mesh_current = monsterParam.Mesh[0];
            Data.mesh_banish = monsterParam.Mesh[1];
            Data.tar_bone = monsterParam.Bone[3];
            Data.shadow_bone[0] = monsterParam.ShadowBone;
            Data.shadow_bone[1] = monsterParam.ShadowBone2;
            btl_util.SetShadow(Data, monsterParam.ShadowX, monsterParam.ShadowZ);
            foreach (SupportingAbilityFeature feature in monsterParam.SupportingAbilityFeatures)
                if (feature.EnableAsMonsterTransform)
                    Data.saMonster.Add(feature);
            btlseq.btlseqinstance seqreader = new btlseq.btlseqinstance();
            btlseq.ReadBattleSequence(btlName, ref seqreader);
            seqreader.FixBuggedAnimations(scene);
            List<AA_DATA> aaList = new List<AA_DATA>();
            List<Int32> usableAbilList = new List<Int32>();
            AA_DATA[] attackAA = [null, null];
            List<Int32>[] attackAnims = [null, null];
            Int32 animOffset = 0;
            String[] battleRawText = FF9TextTool.GetBattleText(FF9BattleDB.SceneData["BSC_" + btlName]);
            if (battleRawText == null)
                battleRawText = [];
            for (i = 0; i < scene.header.AtkCount; i++)
            {
                if (seqreader.GetEnemyIndexOfSequence(i) != monsterIndex)
                    continue;
                AA_DATA ability = scene.atk[i];
                aaList.Add(ability);
                // Swap the TargetType but keep the DefaultAlly flag since it is on by default only for curative/buffing enemy spells
                if (ability.Info.Target == TargetType.AllAlly)
                    ability.Info.Target = TargetType.AllEnemy;
                else if (ability.Info.Target == TargetType.AllEnemy)
                    ability.Info.Target = TargetType.AllAlly;
                else if (ability.Info.Target == TargetType.ManyAlly)
                    ability.Info.Target = TargetType.ManyEnemy;
                else if (ability.Info.Target == TargetType.ManyEnemy)
                    ability.Info.Target = TargetType.ManyAlly;
                else if (ability.Info.Target == TargetType.RandomAlly)
                    ability.Info.Target = TargetType.RandomEnemy;
                else if (ability.Info.Target == TargetType.RandomEnemy)
                    ability.Info.Target = TargetType.RandomAlly;
                else if (ability.Info.Target == TargetType.SingleAlly)
                    ability.Info.Target = TargetType.SingleEnemy;
                else if (ability.Info.Target == TargetType.SingleEnemy)
                    ability.Info.Target = TargetType.SingleAlly;
                if (scene.header.TypCount + i < battleRawText.Length)
                    ability.Name = battleRawText[scene.header.TypCount + i];
                animOffset = seqreader.seq_work_set.AnmOfsList[i];
                Int32 sequenceSfx = seqreader.GetSFXOfSequence(i, out Boolean sequenceChannel, out Boolean sequenceContact);
                if (sequenceSfx >= 0)
                    ability.Info.VfxIndex = (Int16)sequenceSfx;
                if (Configuration.Battle.SFXRework && ability.Info.VfxAction == null)
                    ability.Info.VfxAction = new UnifiedBattleSequencer.BattleAction(scene, seqreader, textid => battleRawText[textid], i);
                if (!ability.MorphDisableAccess && (ability.MorphForceAccess || ability.Ref.ScriptId != 64)) // 64 (no effect) is usually scripted dialogs
                {
                    if (sequenceSfx >= 0 && sequenceContact && !ability.MorphForceAccess)
                    {
                        attackAA[ability.AlternateIdleAccess ? 1 : 0] = ability;
                        attackAnims[ability.AlternateIdleAccess ? 1 : 0] = seqreader.GetAnimationsOfSequence(i);
                    }
                    else
                    {
                        usableAbilList.Add(aaList.Count - 1);
                    }
                }
            }
            CharacterCommands.Commands[commandAsMonster].Type = CharacterCommandType.Ability;
            CharacterCommands.Commands[commandAsMonster].ListEntry = usableAbilList.ToArray();
            Data.is_monster_transform = true;
            UIManager.Battle.ClearCursorMemorize(Position, commandAsMonster);
            btl_cmd.KillSpecificCommand(Data, BattleCommandId.Attack);
            BTL_DATA.MONSTER_TRANSFORM monsterTransform = (Data.monster_transform = new BTL_DATA.MONSTER_TRANSFORM());
            monsterTransform.base_command = commandToReplace;
            monsterTransform.new_command = commandAsMonster;
            monsterTransform.attack = attackAA;
            monsterTransform.spell = aaList;
            monsterTransform.replace_point = updatePts;
            monsterTransform.replace_stat = updateStat;
            monsterTransform.replace_defence = updateDef;
            monsterTransform.replace_element = updateElement;
            monsterTransform.cancel_on_death = cancelOnDeath;
            monsterTransform.death_sound = monsterParam.DieSfx;
            monsterTransform.fade_counter = 0;
            for (i = 0; i < 3; i++)
                monsterTransform.cam_bone[i] = monsterParam.Bone[i];
            for (i = 0; i < 6; i++)
            {
                monsterTransform.icon_bone[i] = monsterParam.IconBone[i];
                monsterTransform.icon_y[i] = monsterParam.IconY[i];
                monsterTransform.icon_z[i] = monsterParam.IconZ[i];
            }
            if (disableCommands == null)
                monsterTransform.disable_commands = new List<BattleCommandId>();
            else
                monsterTransform.disable_commands = disableCommands;
            // Let the spell sequence handle the model fadings (in and out)
            //Data.SetActiveBtlData(false);
            String geoName = FF9BattleDB.GEO.GetValue(monsterParam.Geo);
            Data.ChangeModel(ModelFactory.CreateModel(geoName, true, true, Configuration.Graphics.ElementsSmoothTexture), monsterParam.Geo);
            if (!btl_eqp.EnemyBuiltInWeaponTable.TryGetValue(monsterParam.Geo, out Int32[] builtInWeapons))
                builtInWeapons = null;
            for (i = 0; i < Data.weaponModels.Count; i++)
            {
                BTL_DATA.WEAPON_MODEL weapon = Data.weaponModels[i];
                btl_eqp.SetupWeaponAttachmentFromMonster(weapon, monsterParam, i);
                weapon.builtin_mode = weapon.geo != null && builtInWeapons != null && builtInWeapons.Contains(weapon.bone);
                if (weapon.geo != null && weapon.bone >= 0)
                    geo.geoAttach(weapon.geo, Data.gameObject, weapon.bone);
            }
            Data.bi.t_gauge = 0;
            if (IsUnderAnyStatus(BattleStatus.Trance))
            {
                Data.stat.permanent &= ~BattleStatus.Trance;
                btl_stat.RemoveStatus(this, BattleStatusId.Trance);
                if (Trance == Byte.MaxValue)
                    Trance = Byte.MaxValue - 1;
            }
            //Data.SetActiveBtlData(true);
            geoName = geoName.Substring(4);
            monsterTransform.motion_normal = Data.mot;
            monsterTransform.motion_alternate = new String[34];
            for (i = 0; i < 34; i++)
            {
                monsterTransform.motion_normal[i] = String.Empty;
                monsterTransform.motion_alternate[i] = String.Empty;
            }
            Boolean useDieDmg = (monsterParam.Flags & 2) != 0;
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_NORMAL] = FF9BattleDB.Animation[monsterParam.Mot[0]];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_DYING] = monsterTransform.motion_normal[0];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_DAMAGE1] = FF9BattleDB.Animation[monsterParam.Mot[2]];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_DAMAGE2] = monsterTransform.motion_normal[2];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_DISABLE] = String.Empty;
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_DOWN_DISABLE] = FF9BattleDB.Animation[monsterParam.Mot[useDieDmg ? 3 : 4]];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD] = monsterTransform.motion_normal[0];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_DEFENCE] = monsterTransform.motion_normal[0];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_COVER] = monsterTransform.motion_normal[0];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_AVOID] = monsterTransform.motion_normal[0];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_ESCAPE] = monsterTransform.motion_normal[0];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_WIN_LOOP] = monsterTransform.motion_normal[0];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_CHANT] = monsterTransform.motion_normal[0];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_STEP_FORWARD] = monsterTransform.motion_normal[0];
            monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_STEP_BACK] = monsterTransform.motion_normal[0];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_NORMAL] = FF9BattleDB.Animation[monsterParam.Mot[1]];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_DYING] = monsterTransform.motion_alternate[0];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_DAMAGE1] = FF9BattleDB.Animation[monsterParam.Mot[3]];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_DAMAGE2] = monsterTransform.motion_alternate[2];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_DISABLE] = String.Empty;
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_DOWN_DISABLE] = FF9BattleDB.Animation[monsterParam.Mot[4]];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_CMD] = monsterTransform.motion_alternate[0];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_DEFENCE] = monsterTransform.motion_alternate[0];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_COVER] = monsterTransform.motion_alternate[0];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_AVOID] = monsterTransform.motion_alternate[0];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_ESCAPE] = monsterTransform.motion_alternate[0];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_WIN_LOOP] = monsterTransform.motion_alternate[0];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_CHANT] = monsterTransform.motion_alternate[0];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_STEP_FORWARD] = monsterTransform.motion_alternate[0];
            monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_STEP_BACK] = monsterTransform.motion_alternate[0];
            // Try to automatically get a few animations
            // Physical attack
            for (i = 0; i < 6; i++)
            {
                if (attackAnims[0] != null && i < attackAnims[0].Count)
                    monsterTransform.motion_normal[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_SET + i] = attackAnims[0][i] == 0xFF ? monsterTransform.motion_normal[0] : FF9BattleDB.Animation[seqreader.seq_work_set.AnmAddrList[animOffset + attackAnims[0][i]]];
                if (attackAnims[1] != null && i < attackAnims[1].Count)
                    monsterTransform.motion_alternate[(Int32)BattlePlayerCharacter.PlayerMotionIndex.MP_SET + i] = attackAnims[1][i] == 0xFF ? monsterTransform.motion_alternate[0] : FF9BattleDB.Animation[seqreader.seq_work_set.AnmAddrList[animOffset + attackAnims[1][i]]];
            }
            // Cast Init / Loop / End
            if (geoName.CompareTo("MON_B3_199") == 0) // Necron
            {
                ChangeToMonster_SetClip(monsterTransform.motion_normal, "ANH_" + geoName + "_030", BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_TO_CHANT);
                ChangeToMonster_SetClip(monsterTransform.motion_normal, "ANH_" + geoName + "_031", BattlePlayerCharacter.PlayerMotionIndex.MP_CHANT);
                ChangeToMonster_SetClip(monsterTransform.motion_normal, "ANH_" + geoName + "_032", BattlePlayerCharacter.PlayerMotionIndex.MP_MAGIC);
                ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_020", BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_TO_CHANT);
                ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_021", BattlePlayerCharacter.PlayerMotionIndex.MP_CHANT);
                ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_022", BattlePlayerCharacter.PlayerMotionIndex.MP_MAGIC);
            }
            else
            {
                ChangeToMonster_SetClip(monsterTransform.motion_normal, "ANH_" + geoName + "_020", BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_TO_CHANT);
                ChangeToMonster_SetClip(monsterTransform.motion_normal, "ANH_" + geoName + "_021", BattlePlayerCharacter.PlayerMotionIndex.MP_CHANT);
                ChangeToMonster_SetClip(monsterTransform.motion_normal, "ANH_" + geoName + "_022", BattlePlayerCharacter.PlayerMotionIndex.MP_MAGIC);
                if (geoName.CompareTo("MON_B3_050") == 0 || geoName.CompareTo("MON_B3_051") == 0 || geoName.CompareTo("MON_B3_061") == 0 || geoName.CompareTo("MON_B3_115") == 0 || geoName.CompareTo("MON_B3_119") == 0 || geoName.CompareTo("MON_B3_120") == 0)
                {
                    // Xylomid [MON_B3_050], Movers [MON_B3_051], Pampa [MON_B3_061], Black Waltz 3 [MON_B3_115], Zorn [MON_B3_119], Thorn [MON_B3_120]
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_040", BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_TO_CHANT);
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_041", BattlePlayerCharacter.PlayerMotionIndex.MP_CHANT);
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_042", BattlePlayerCharacter.PlayerMotionIndex.MP_MAGIC);
                }
                else if (geoName.CompareTo("MON_B3_126") == 0 || geoName.CompareTo("MON_B3_167") == 0)
                {
                    // Tantarian [MON_B3_126], Gimme Cat [MON_B3_167]
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_050", BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_TO_CHANT);
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_051", BattlePlayerCharacter.PlayerMotionIndex.MP_CHANT);
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_052", BattlePlayerCharacter.PlayerMotionIndex.MP_MAGIC);
                }
                else if (geoName.CompareTo("MON_B3_146") == 0)
                {
                    // Hades [MON_B3_146]
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_080", BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_TO_CHANT);
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_081", BattlePlayerCharacter.PlayerMotionIndex.MP_CHANT);
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_082", BattlePlayerCharacter.PlayerMotionIndex.MP_MAGIC);
                }
                else
                {
                    // Serpion [MON_B3_046], Torama [MON_B3_082], Lich [MON_B3_140], Crystal Lich [MON_B3_191], Deathguise [MON_B3_147], Gargoyle (?) [MON_B3_072]
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_030", BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_TO_CHANT);
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_031", BattlePlayerCharacter.PlayerMotionIndex.MP_CHANT);
                    ChangeToMonster_SetClip(monsterTransform.motion_alternate, "ANH_" + geoName + "_032", BattlePlayerCharacter.PlayerMotionIndex.MP_MAGIC);
                }
            }
            // Duplicate existing animations or create dummy ones but have different names for btl_mot.checkMotion proper functioning
            HashSet<String> uniqueAnimList = new HashSet<String>();
            for (i = 0; i < 68; i++)
            {
                String baseName = i < 34 ? monsterTransform.motion_normal[i] : monsterTransform.motion_alternate[i - 34];
                if (String.IsNullOrEmpty(baseName) || uniqueAnimList.Contains(baseName))
                {
                    String newName = "ANH_" + geoName + "_DUMMY_" + i;
                    if (!String.IsNullOrEmpty(baseName) && Data.gameObject.GetComponent<Animation>().GetClip(baseName) != null)
                        Data.gameObject.GetComponent<Animation>().AddClip(Data.gameObject.GetComponent<Animation>().GetClip(baseName), newName);
                    else
                        AnimationClipReader.CreateDummyAnimationClip(Data.gameObject, newName);
                    if (i < 34)
                        monsterTransform.motion_normal[i] = newName;
                    else
                        monsterTransform.motion_alternate[i - 34] = newName;
                }
                else
                {
                    if (Data.gameObject.GetComponent<Animation>().GetClip(baseName) == null)
                        AnimationClipReader.CreateDummyAnimationClip(Data.gameObject, baseName);
                    uniqueAnimList.Add(baseName);
                }
            }
            Data.bi.def_idle = 0;
            btl_mot.setMotion(Data, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_NORMAL);
            // Add monster statuses and status resistances
            BattleStatus current_added = 0;
            monsterTransform.resist_added = 0;
            monsterTransform.auto_added = 0;
            foreach (SupportingAbilityFeature saFeature in Data.saMonster)
            {
                saFeature.GetStatusInitQuietly(this, out BattleStatus permanent, out BattleStatus initial, out BattleStatus resist, out StatusModifier partialResist, out StatusModifier durationFactor, out Int16 atb);
                current_added |= initial;
                monsterTransform.resist_added |= resist;
                monsterTransform.auto_added |= permanent;
            }
            btl_stat.RemoveStatuses(this, monsterTransform.resist_added);
            monsterTransform.resist_added &= ~ResistStatus;
            monsterTransform.auto_added &= ~PermanentStatus;
            ResistStatus |= monsterTransform.resist_added;
            monsterTransform.auto_added &= ~ResistStatus;
            btl_stat.AlterStatuses(this, current_added);
            btl_stat.MakeStatusesPermanent(this, monsterTransform.auto_added, true);
            // TODO: handle "partialResist" and "durationFactor" properly (now, they are most likely applied but persist after "ReleaseChangeToMonster")
            btl2d.ShowMessages(true);
        }

        private void ChangeToMonster_SetClip(String[] array, String animName, BattlePlayerCharacter.PlayerMotionIndex motion)
        {
            if (Data.gameObject.GetComponent<Animation>().GetClip(animName) != null)
                array[(Int32)motion] = animName;
        }

        public void ReleaseChangeToMonster()
        {
            BTL_DATA.MONSTER_TRANSFORM monsterTransform = Data.monster_transform;
            PLAYER p = FF9StateSystem.Common.FF9.party.member[Position];
            CharacterBattleParameter btlParam = btl_mot.BattleParameterList[p.info.serial_no];
            btl_stat.RemoveStatuses(this, BattleStatusConst.RemoveOnMonsterTransform);
            if (monsterTransform.replace_point)
            {
                Data.max.hp = p.max.hp;
                Data.max.mp = p.max.mp;
                Data.cur.hp = Math.Min(Data.cur.hp, Data.max.hp);
                Data.cur.mp = Math.Min(Data.cur.mp, Data.max.mp);
            }
            if (monsterTransform.replace_stat)
            {
                Strength = p.elem.str;
                Magic = p.elem.mgc;
                Dexterity = p.elem.dex;
                Will = p.elem.wpr;
            }
            if (monsterTransform.replace_defence)
            {
                Data.defence.PhysicalDefence = p.defence.PhysicalDefence;
                Data.defence.PhysicalEvade = p.defence.PhysicalEvade;
                Data.defence.MagicalDefence = p.defence.MagicalDefence;
                Data.defence.MagicalEvade = p.defence.MagicalEvade;
            }
            if (monsterTransform.replace_element)
                btl_eqp.InitEquipPrivilegeAttrib(p, Data);
            Data.is_monster_transform = false;
            ResistStatus &= ~monsterTransform.resist_added;
            btl_stat.MakeStatusesPermanent(this, monsterTransform.auto_added, false);
            Data.mesh_current = 0;
            Data.mesh_banish = UInt16.MaxValue;
            Data.tar_bone = 0;
            Data.shadow_bone[0] = btlParam.ShadowData[0];
            Data.shadow_bone[1] = btlParam.ShadowData[1];
            btl_util.SetShadow(Data, btlParam.ShadowData[2], btlParam.ShadowData[3]);
            Data.saMonster.Clear();
            btl_cmd.KillSpecificCommand(Data, monsterTransform.new_command);
            btl_cmd.KillSpecificCommand(Data, BattleCommandId.EnemyCounter);
            Data.geo_scale_default = 4096;
            geo.geoScaleReset(Data);
            if (battle.TRANCE_GAUGE_FLAG != 0 && (p.category & 16) == 0 && (Data.bi.slot_no != (Byte)CharacterId.Garnet || battle.GARNET_DEPRESS_FLAG == 0))
                Data.bi.t_gauge = 1;
            // Reset the position even when ChangeToMonster doesn't change it by itself
            Data.pos.x = Data.evt.posBattle.x = Data.evt.pos[0] = Data.base_pos.x = Data.original_pos.x;
            Data.pos.y = Data.evt.posBattle.y = Data.evt.pos[1] = Data.base_pos.y = Data.original_pos.y;
            Data.pos.z = Data.evt.posBattle.z = Data.evt.pos[2] = Data.base_pos.z = Data.original_pos.z + (Data.bi.row == 0 ? -400 : 0);
            Data.mot = monsterTransform.motion_normal;
            for (Int32 i = 0; i < 34; i++)
                Data.mot[i] = btlParam.AnimationId[i];
            if (Data.cur.hp == 0)
                btl_mot.setMotion(Data, BattlePlayerCharacter.PlayerMotionIndex.MP_DISABLE);
            else
                btl_mot.setMotion(Data, BattlePlayerCharacter.PlayerMotionIndex.MP_IDLE_NORMAL);
            Data.evt.animFrame = 0;
            if (Data.weaponModels.Count > 0)
                Data.weaponModels[0].builtin_mode = false;
            btl_vfx.SetTranceModel(Data, false);
            btl_mot.HideMesh(Data, UInt16.MaxValue);
            monsterTransform.fade_counter = 2;
            UIManager.Battle.ClearCursorMemorize(Position, monsterTransform.new_command);
        }

        public Object GetPropertyByName(String propertyName)
        {
            switch (propertyName)
            {
                case "Name": return Name;
                case "UnitId": return (Int32)Id;
                case "MaxHP": return MaximumHp;
                case "MaxMP": return MaximumMp;
                case "MaxATB": return (Int32)MaximumAtb;
                case "HP": return CurrentHp;
                case "MP": return CurrentMp;
                case "MaxDamageLimit": return MaxDamageLimit;
                case "MaxMPDamageLimit": return MaxMpDamageLimit;
                case "ATB": return (Int32)CurrentAtb;
                case "Trance": return (Int32)Trance;
                case "InTrance": return InTrance;
                case "CurrentStatus": return (UInt64)CurrentStatus;
                case "PermanentStatus": return (UInt64)PermanentStatus;
                case "ResistStatus": return (UInt64)ResistStatus;
                case "HalfElement": return (Int32)HalfElement;
                case "GuardElement": return (Int32)GuardElement;
                case "AbsorbElement": return (Int32)AbsorbElement;
                case "WeakElement": return (Int32)WeakElement;
                case "BonusElement": return (Int32)BonusElement;
                case "WeaponPower": return WeaponPower;
                case "WeaponRate": return WeaponRate;
                case "WeaponElement": return (Int32)WeaponElement;
                case "WeaponStatus": return (UInt64)WeaponStatus;
                case "WeaponCategory": return (Int32)WeapCategory;
                case "SerialNumber": return (Int32)SerialNumber;
                case "Row": return (Int32)Row;
                case "Position": return (Int32)Position;
                case "SummonCount": return (Int32)SummonCount;
                case "IsPlayer": return IsPlayer;
                case "IsSlave": return IsSlave;
                case "IsOutOfReach": return IsOutOfReach;
                case "Level": return (Int32)Level;
                case "Exp": return IsPlayer ? Player.exp : 0u;
                case "Speed": return (Int32)Dexterity;
                case "Strength": return (Int32)Strength;
                case "Magic": return (Int32)Magic;
                case "Spirit": return (Int32)Will;
                case "Defence": return PhysicalDefence;
                case "Evade": return PhysicalEvade;
                case "MagicDefence": return MagicDefence;
                case "MagicEvade": return MagicEvade;
                case "PlayerCategory": return (Int32)PlayerCategory;
                case "Category": return (Int32)Category;
                case "CharacterIndex": return (Int32)PlayerIndex;
                case "IsAlternateStand": return Data.bi.def_idle == 1 && (!IsPlayer || IsMonsterTransform);
                case "CriticalRateBonus": return (Int32)CriticalRateBonus;
                case "CriticalRateResistance": return (Int32)CriticalRateResistance;
                case "WeaponId": return (Int32)Weapon;
                case "HeadId": return (Int32)Head;
                case "WristId": return (Int32)Wrist;
                case "ArmorId": return (Int32)Armor;
                case "AccessoryId": return (Int32)Accessory;
                case "ModelId": return (Int32)Data.dms_geo_id;
                case "BonusExp": return IsPlayer ? 0 : Enemy.BonusExperience;
                case "BonusGil": return IsPlayer ? 0 : Enemy.BonusGil;
                case "BonusCard": return IsPlayer ? 0 : (Int32)Enemy.DroppableCard;
                case "StealableItemCount": return IsPlayer ? 0 : Enemy.StealableItems.Count(p => p != RegularItem.NoItem);
            }
            if (propertyName.StartsWith("StatusProperty "))
            {
                String[] token = propertyName.Split(' ');
                if (token.Length < 3)
                {
                    Log.Error($"[BattleUnit] Invalid status property access \"{propertyName}\"");
                    return -1;
                }
                if (!token[1].TryEnumParse(out BattleStatusId statusId))
                    return 0;
                Object result = GetCurrentStatusEffectScript(statusId)?.GetFieldValue<Object>(token[2]);
                if (result == null)
                    return 0;
                if (result is Enum)
                    return Convert.ToUInt64(result);
                return result;
            }
            if (propertyName.StartsWith("HasSA ") && Int32.TryParse(propertyName.Substring("HasSA ".Length), out Int32 supportId))
                return HasSupportAbilityByIndex((SupportAbility)supportId);
            if (propertyName.StartsWith("CanUseAbility ") && Int32.TryParse(propertyName.Substring("CanUseAbility ".Length), out Int32 abilId))
                return IsAbilityAvailable((BattleAbilityId)abilId);
            if (propertyName.StartsWith("HasLearntAbility ") && Int32.TryParse(propertyName.Substring("HasLearntAbility ".Length), out Int32 aaId))
                return HasLearntAbility((BattleAbilityId)aaId);
            if (propertyName.StartsWith("HasLearntSupport ") && Int32.TryParse(propertyName.Substring("HasLearntSupport ".Length), out Int32 saId))
                return HasLearntAbility((SupportAbility)saId);
            Log.Error($"[BattleUnit] Unrecognized unit property \"{propertyName}\"");
            return -1;
        }
    }
}
