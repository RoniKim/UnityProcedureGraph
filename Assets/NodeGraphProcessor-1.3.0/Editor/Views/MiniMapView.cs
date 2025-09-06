using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace GraphProcessor
{
	public class MiniMapView : MiniMap
	{
		new BaseGraphView	graphView;
		Vector2				size;

		public MiniMapView(BaseGraphView baseGraphView)
		{
			this.graphView = baseGraphView;
			SetPosition(new Rect(10, 30, 200, 200));
			size = new Vector2(200, 200);
		}
	}
}