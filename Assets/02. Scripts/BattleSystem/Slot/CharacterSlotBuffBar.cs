using UnityEngine;
using System.Collections.Generic;
using Buffs;

namespace Shapes {

	public class CharacterSlotBuffBar : MonoBehaviour {

		[Header("Prefabs")]
		[SerializeField] private GameObject buffPrefab;
		[SerializeField] private GameObject debuffPrefab;

		[Header("Layout Settings")]
		[SerializeField] private float startX = 10f;
		[SerializeField] private float spacing = 35f;

		private List<GameObject> _spawnedIcons = new List<GameObject>();
		private IReadOnlyList<ActiveBuff> _activeBuffs = null;

		public void SetBuffs(IReadOnlyList<ActiveBuff> buffs) {
			_activeBuffs = buffs;
			ClearSpawnedIcons();

			if (_activeBuffs == null || _activeBuffs.Count == 0) return;

			List<string> buffLines = new List<string>();
			List<string> debuffLines = new List<string>();

			foreach (var buff in _activeBuffs) {
				// 1. 기존 스탯 버프/디버프 처리
				if (buff.Modifiers != null && buff.Modifiers.Count > 0) {
					var targetMod = buff.Modifiers.Find(m => m.type == StatType.STR || m.type == StatType.INT || m.type == StatType.AGI);
					if (targetMod != null) {
						string statName = targetMod.type switch {
							StatType.STR => "<color=#E63333>힘</color>",
							StatType.INT => "<color=#3380E6>지능</color>",
							StatType.AGI => "<color=#33CC4D>민첩</color>",
							_ => "스탯"
						};

						if (targetMod.value >= 0) {
							string sign = "+";
							buffLines.Add($"{statName} {sign}{targetMod.value} ({buff.RemainingTurns}턴 지속)");
						} else {
							debuffLines.Add($"{statName} {targetMod.value} ({buff.RemainingTurns}턴 지속)");
						}
					}
				}

				// 2. 도트 뎀/힐 처리
				if (buff.HpChangePerTurn != 0) {
					int amount = Mathf.RoundToInt(buff.HpChangePerTurn);
					if (amount > 0) {
						buffLines.Add($"<color=#33CCCC>도트 힐</color> +{amount} ({buff.RemainingTurns}턴 지속)");
					} else {
						debuffLines.Add($"<color=#CC33C2>도트 피해</color> {amount} ({buff.RemainingTurns}턴 지속)");
					}
				}
			}

			float xOffset = 0f;

			// 1. 버프 그룹 생성 (양수 효과 모음)
			if (buffLines.Count > 0 && buffPrefab != null) {
				GameObject buffObj = Instantiate(buffPrefab, transform);
				_spawnedIcons.Add(buffObj);

				RectTransform rt = buffObj.GetComponent<RectTransform>();
				if (rt != null) {
					rt.anchoredPosition = new Vector2(startX + xOffset, 0f);
				}

				BuffIcon buffIcon = buffObj.GetComponent<BuffIcon>();
				if (buffIcon == null) {
					buffIcon = buffObj.AddComponent<BuffIcon>();
				}
				buffIcon.Setup(string.Join("\n", buffLines), this);

				xOffset += spacing;
			}

			// 2. 디버프 그룹 생성 (음수 효과 모음)
			if (debuffLines.Count > 0 && debuffPrefab != null) {
				GameObject debuffObj = Instantiate(debuffPrefab, transform);
				_spawnedIcons.Add(debuffObj);

				RectTransform rt = debuffObj.GetComponent<RectTransform>();
				if (rt != null) {
					rt.anchoredPosition = new Vector2(startX + xOffset, 0f);
				}

				BuffIcon buffIcon = debuffObj.GetComponent<BuffIcon>();
				if (buffIcon == null) {
					buffIcon = debuffObj.AddComponent<BuffIcon>();
				}
				buffIcon.Setup(string.Join("\n", debuffLines), this);
			}
		}

		private void ClearSpawnedIcons() {
			foreach (var icon in _spawnedIcons) {
				if (icon != null) {
					Destroy(icon);
				}
			}
			_spawnedIcons.Clear();
		}
	}
}
