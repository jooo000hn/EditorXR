﻿using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.VR.Utilities;

public class ObjectPlacementModule : MonoBehaviour, ISpatialHash
{
	[SerializeField]
	float kInstantiateFOVDifference = -10f;

	const float kGrowDuration = 0.5f;

	public System.Action<Object> addObjectToSpatialHash { get; set; }
	public System.Action<Object> removeObjectFromSpatialHash { get; set; }

	public void Preview(Transform preview, Transform previewOrigin, float t = 1f, Quaternion? localRotation = null)
	{
		preview.transform.position = Vector3.Lerp(preview.transform.position, previewOrigin.position, t);
		preview.transform.rotation = Quaternion.Lerp(
									preview.transform.rotation,
									localRotation != null ? previewOrigin.rotation * localRotation.Value : previewOrigin.rotation,
									t);
	}

	public void PlaceObject(Transform obj, Vector3 targetScale)
	{
		StartCoroutine(PlaceObjectCoroutine(obj, targetScale));
	}

	private IEnumerator PlaceObjectCoroutine(Transform obj, Vector3 targetScale)
	{
		// Don't let us direct select while placing
		removeObjectFromSpatialHash(obj);

		float start = Time.realtimeSinceStartup;
		var currTime = 0f;

		obj.parent = null;
		var startScale = obj.localScale;
		var startPosition = obj.position;

		//Get bounds at target scale
		var origScale = obj.localScale;
		obj.localScale = targetScale;
		var totalBounds = U.Object.GetTotalBounds(obj);
		obj.localScale = origScale;

		if (totalBounds != null)
		{
			// We want to position the object so that it fits within the camera perspective at its original scale
			var camera = U.Camera.GetMainCamera();
			var halfAngle = camera.fieldOfView * 0.5f;
			var perspective = halfAngle + kInstantiateFOVDifference;
			var camPosition = camera.transform.position;
			var forward = obj.position - camPosition;
			forward.y = 0;

			var distance = totalBounds.Value.size.magnitude / Mathf.Tan(perspective * Mathf.Deg2Rad);
			var destinationPosition = obj.position;
			if (distance > forward.magnitude)
				destinationPosition = camPosition + forward.normalized * distance;

			while (currTime < kGrowDuration)
			{
				currTime = Time.realtimeSinceStartup - start;
				var t = currTime / kGrowDuration;
				var tSquared = t * t;
				obj.localScale = Vector3.Lerp(startScale, targetScale, tSquared);
				obj.position = Vector3.Lerp(startPosition, destinationPosition, tSquared);
				yield return null;
			}
			obj.localScale = targetScale;
		}
		Selection.activeGameObject = obj.gameObject;

		addObjectToSpatialHash(obj);
	}
}