using UnityEngine;

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

		public override void DrawPanelShapes( Rect rect, ImCanvasContext ctx ) {
			if( colorGradient1 == null || colorGradient2 == null ) 
				return; // just in case it hasn't initialized

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
			Draw.Rectangle( hpfillRect, hpCol );
			
			// Draw the colored bar (Ins):
			Rect insfillRect = innerRect;
			insfillRect.height = barHeight;
			insfillRect.width *= insfillAmount;
			Color insCol = colorGradient2.Evaluate( insfillAmount );
			insCol.a *= alpha;
			Draw.Rectangle( insfillRect, insCol );
		}

		Rect Inset( Rect r, float amount ) {
			return new Rect( r.x + amount, r.y + amount, r.width - amount * 2, r.height - amount * 2 );
		}

	}

}