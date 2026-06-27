using UnityEngine;
using System.Collections.Generic;
using Buffs;
using BattleSystem;

namespace Shapes {

	public class CharacterSlotBar : ImmediateModePanel {

		[Range( 0, 1 )]
		public float hpfillAmount = 1;
		[Range( 0, 1 )]
		public float insfillAmount = 1;
		public Gradient colorGradient1;
		public Gradient colorGradient2;
		public Color currentColor = Color.gray;
		[Range( 0, 1 )]
		public float alpha = 1f;
		public Color defaultBorderColor = new Color(0.5f, 0.5f, 0.5f, 1f);

		[Header("Turn Indicator Settings")]
		[SerializeField] private Rectangle turnIndicatorRect;
		[SerializeField] private float blinkSpeed = 4f;

		private bool _isTurnActive = false;
		public bool isTurnActive {
			get => _isTurnActive;
			set {
				if (_isTurnActive != value) {
					_isTurnActive = value;
					if (turnIndicatorRect != null) {
						turnIndicatorRect.gameObject.SetActive(value);
					}
				}
			}
		}

		private void Start() {
			// [방어 코드] 씬/프리팹 직렬화 값 꼬임으로 alpha가 0이 되어 UI가 안 보이는 버그 예방
			alpha = 1f;

			if (turnIndicatorRect != null) {
				CharacterSlot slot = GetComponentInParent<CharacterSlot>();
				_isTurnActive = slot != null && slot.IsActing;
				turnIndicatorRect.gameObject.SetActive(_isTurnActive);
			}
		}

		private void Update() {
			if (_isTurnActive && turnIndicatorRect != null) {
				Color col = turnIndicatorRect.Color;
				col.a = 0.5f + Mathf.Sin(Time.unscaledTime * blinkSpeed) * 0.3f;
				turnIndicatorRect.Color = col;
			}
		}

		public override void DrawPanelShapes( Rect rect, ImCanvasContext ctx ) {
			// [방어 코드] 빌드본 번들 패키징 시 그라디언트 참조 유실(null) 대응
			if (colorGradient1 == null) {
				colorGradient1 = new Gradient();
				colorGradient1.SetKeys(
					new GradientColorKey[] { new GradientColorKey(new Color(0.7f, 0f, 0f), 0f), new GradientColorKey(new Color(1f, 0.33f, 0f), 1f) },
					new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
				);
			}
			if (colorGradient2 == null) {
				colorGradient2 = new Gradient();
				colorGradient2.SetKeys(
					new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.9f, 0.85f, 0.13f), 1f) },
					new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
				);
			}

			// [방어 코드] 초기 기동 시 또는 비정상적인 상태로 alpha가 0f인 경우 보정
			if (alpha <= 0.01f) {
				alpha = 1f;
			}

			// Draw black background:
			Draw.Rectangle( rect, 8f, new Color( 0, 0, 0, alpha ) );

			// Draw border:
			Color borderCol = currentColor;
			borderCol.a *= alpha;
			Draw.RectangleBorder( rect, 3f, 8f, borderCol );

			Rect innerRect = Inset( rect, 8 );
			float spacing = 4f;
			float barHeight = ( innerRect.height - spacing ) / 2f;

			// Draw the colored bar (HP):
			Rect hpfillRect = innerRect;
			hpfillRect.y += barHeight + spacing;
			hpfillRect.height = barHeight;
			hpfillRect.width *= hpfillAmount;
			Color hpCol = colorGradient1.Evaluate( hpfillAmount );
			hpCol.a *= alpha;
			Draw.Rectangle( hpfillRect, 4f, hpCol );
			
			// Draw the colored bar (Ins):
			Rect insfillRect = innerRect;
			insfillRect.height = barHeight;
			insfillRect.width *= insfillAmount;
			Color insCol = colorGradient2.Evaluate( insfillAmount );
			insCol.a *= alpha;
			Draw.Rectangle( insfillRect, 4f, insCol );
		}

		Rect Inset( Rect r, float amount ) {
			return new Rect( r.x + amount, r.y + amount, r.width - amount * 2, r.height - amount * 2 );
		}

	}

}