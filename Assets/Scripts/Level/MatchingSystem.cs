﻿/* author: Brian Tria
 * created: Dec 14, 2019
 * description: Using Offset Coordinates for layout. see: https://www.redblobgames.com/grids/hexagons/
 *				We're using Odd-Offset row coordinates.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchingSystem : MonoBehaviour
{
	#region Member Variables
	// known issue for SerializedField throwing warnings
	// link: https://forum.unity.com/threads/serializefield-warnings.560878/
#pragma warning disable 0649
	[Header("References")]
	[SerializeField] private IntVariable bubbleBulletType;
	[SerializeField] private IntVariable minimumMatchCount;
	[SerializeField] private IntVariableList levelDataMap;
	[SerializeField] private VectorVariable bubbleSize;
	[SerializeField] private VectorVariable levelMapDimension;
	[SerializeField] private GameObjectList activeBubbleObjectList;
	[SerializeField] private GameObjectList inactiveBubbleObjectList;
	[SerializeField] private VectorVariable bulletHitPosition;
	[SerializeField] private VectorVariable bubbleHitCoordinates;

	[Header("Game Events")]
	[SerializeField] private GameEvent onBulletReload;
#pragma warning restore 0649

	int[,,] neighborOffsetArray = new int[,,]
	{
		{
			{1, 0}, {0, -1}, {-1, -1},
			{-1, 0}, {-1, 1}, {0, 1}
		},
		{
			{1, 0}, {1, -1}, {0, -1},
			{-1, 0}, {0, 1}, {1, 1}
		}
	};
	#endregion

	#region Private Methods
	bool HasMissingReference()
	{
		if (bubbleBulletType == null)
		{
			Debug.LogError("Missing reference to bubble bullet type.");
			return true;
		}

		if (minimumMatchCount == null)
		{
			Debug.LogError("Missing reference to minimum match count.");
			return true;
		}

		if (bulletHitPosition == null)
		{
			Debug.LogError("Missing reference to bullet hit position.");
			return true;
		}

		if (bubbleSize == null)
		{
			Debug.LogError("Missing reference to bubble size.");
			return true;
		}

		if (bubbleHitCoordinates == null)
		{
			Debug.LogError("Missing reference to bullet hit coordinates.");
			return true;
		}

		if (activeBubbleObjectList == null)
		{
			Debug.LogError("Missing reference to active bubble object list.");
			return true;
		}

		if (inactiveBubbleObjectList == null)
		{
			Debug.LogError("Missing reference to inactive bubble object list.");
			return true;
		}

		if (levelDataMap == null)
		{
			Debug.LogError("Missing reference to level data map.");
			return true;
		}

		if (levelMapDimension == null)
		{
			Debug.LogError("Missing reference to level map dimension.");
			return true;
		}

		return false;
	}
	#endregion

	// TODO: modular implementation
	public void PopMatchedBubbles()
	{
		if (HasMissingReference())
		{
			return;
		}

		// (1) find bullet attach point
		List<Vector2> blankNeighborCoordinates = new List<Vector2>();
		Vector2 hitCoordinates = bubbleHitCoordinates.RuntimeValue;
		int levelMapColumnCount = (int)levelMapDimension.RuntimeValue.x;
		int levelMapRowCount = (int)levelMapDimension.RuntimeValue.y;

		// Debug.Log("map dimension: " + levelMapDimension.RuntimeValue);
		// Debug.Log("hit coord: " + hitCoordinates);

		int neighborOffsetType = (int)hitCoordinates.y % 2;
		int neighborOffsetPairCount = neighborOffsetArray.GetLength(1);

		for (int pairIdx = 0; pairIdx < neighborOffsetPairCount; ++pairIdx)
		{
			int neighborX = (int)hitCoordinates.x + neighborOffsetArray[neighborOffsetType, pairIdx, 0];
			int neighborY = (int)hitCoordinates.y + neighborOffsetArray[neighborOffsetType, pairIdx, 1];
			int neighborIdx = (neighborY * levelMapColumnCount) + (neighborX % levelMapColumnCount);
			// Debug.Log("(" + neighborX + "," + neighborY + ")" + " => " + neighborIdx);

			if (neighborIdx < 0 || neighborIdx >= levelDataMap.RuntimeContents.Count)
			{
				// Debug.Log("<b>adjust for</b>: (" + neighborX + ", " + neighborY + ")");
				continue;
			}

			if (levelDataMap.RuntimeContents[neighborIdx] == 0)
			{
				// Debug.Log("<b>add</b>: (" + neighborX + ", " + neighborY + ")");
				blankNeighborCoordinates.Add(new Vector2(neighborX, neighborY));
			}
			// else
			// {
			// 	Debug.Log("<b>skip</b>: (" + neighborX + ", " + neighborY + ")");
			// }
		}

		Vector3 hitPosition = bulletHitPosition.RuntimeValue;
		Vector2 hitPostion2D = new Vector2(hitPosition.x, hitPosition.y);
		Vector2 attachCoordinate = Vector2.zero;
		Vector3 attachPosition = Vector3.zero;

		float closestDistance = float.MaxValue;
		foreach (Vector2 blankCoordinate in blankNeighborCoordinates)
		{
			// TODO: create a utility function
			float positionY = blankCoordinate.y * bubbleSize.RuntimeValue.y;
			float positionX = -(levelMapColumnCount / 2) * bubbleSize.RuntimeValue.x;
			positionX += bubbleSize.RuntimeValue.x * blankCoordinate.x;

			// offset odd-indexed rows 
			if ((int)blankCoordinate.y % 2 > 0)
			{
				positionX += bubbleSize.RuntimeValue.x * 0.5f;
			}

			Vector2 blankPosition = new Vector2(positionX, positionY);
			// Debug.Log("blank position: " + blankPosition);

			float distance = Vector2.Distance(hitPostion2D, blankPosition);

			if (distance < closestDistance)
			{
				attachPosition.x = positionX;
				attachPosition.y = positionY;
				attachCoordinate = blankCoordinate;
				closestDistance = distance;
			}
		}

		// for debugging
		//attachPosition = hitPosition;


		GameObject bubbleObject = inactiveBubbleObjectList.Contents[0];

		Bubble bubble = bubbleObject.GetComponent<Bubble>();
		if (bubble != null)
		{
			inactiveBubbleObjectList.Contents.RemoveAt(0);
			activeBubbleObjectList.Contents.Add(bubbleObject);

			bubble.Type = (BubbleType)bubbleBulletType.RuntimeValue;
			bubble.Coordinates = attachCoordinate;

			bubbleObject.transform.localPosition = attachPosition;
			bubbleObject.SetActive(true);
			Debug.Log("attach position: " + attachPosition);
		}
		else
		{
			Debug.LogError("Missing bubble component.");
		}

		// (2) check for matching bubbles; bullet as root node
		// (3) pop if match count is valid

		// TODO: check matches
		// TODO: loop to pop neighboring bubbles

		if (onBulletReload != null)
		{
			onBulletReload.Raise();
		}
	}
}
