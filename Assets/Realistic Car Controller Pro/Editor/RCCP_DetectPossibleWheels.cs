//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RCCP_DetectPossibleWheels : Editor {

    public struct WheelClassification {

        [Tooltip("Detected front-left wheel GameObject.")]
        public GameObject frontLeft;
        [Tooltip("Detected front-right wheel GameObject.")]
        public GameObject frontRight;
        [Tooltip("Detected rear-left wheel GameObject.")]
        public GameObject rearLeft;
        [Tooltip("Detected rear-right wheel GameObject.")]
        public GameObject rearRight;
        [Tooltip("Wheel candidates that could not be classified by position.")]
        public GameObject[] unassigned;
        [Tooltip("Total number of wheel candidates found on the vehicle.")]
        public int totalCandidates;

    }

    public static WheelClassification ClassifyWheels(GameObject vehicle) {

        GameObject[] candidates = DetectPossibleAllWheels(vehicle);

        WheelClassification result = new WheelClassification {
            totalCandidates = candidates.Length,
            unassigned = System.Array.Empty<GameObject>()
        };

        if (candidates.Length == 0)
            return result;

        // Convert each candidate to vehicle-local space for quadrant assignment.
        Vector3[] localPositions = new Vector3[candidates.Length];
        for (int i = 0; i < candidates.Length; i++)
            localPositions[i] = vehicle.transform.InverseTransformPoint(candidates[i].transform.position);

        if (candidates.Length <= 4) {

            // Assign by quadrant: Z>0 = front, X<0 = left.
            for (int i = 0; i < candidates.Length; i++) {

                bool isFront = localPositions[i].z > 0;
                bool isRight = localPositions[i].x > 0;

                if (isFront && !isRight && result.frontLeft == null)
                    result.frontLeft = candidates[i];
                else if (isFront && isRight && result.frontRight == null)
                    result.frontRight = candidates[i];
                else if (!isFront && !isRight && result.rearLeft == null)
                    result.rearLeft = candidates[i];
                else if (!isFront && isRight && result.rearRight == null)
                    result.rearRight = candidates[i];

            }

        } else {

            // More than 4 candidates — greedy nearest-corner assignment.
            // Compute approximate corners from renderer bounds.
            Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (Renderer r in renderers) {

                Bounds rb = r.bounds;
                Vector3 localMin = vehicle.transform.InverseTransformPoint(rb.min);
                Vector3 localMax = vehicle.transform.InverseTransformPoint(rb.max);
                localBounds.Encapsulate(localMin);
                localBounds.Encapsulate(localMax);

            }

            // Ideal corner positions (XZ plane, Y ignored).
            Vector3[] corners = new Vector3[] {
                new Vector3(localBounds.min.x, 0, localBounds.max.z), // FL
                new Vector3(localBounds.max.x, 0, localBounds.max.z), // FR
                new Vector3(localBounds.min.x, 0, localBounds.min.z), // RL
                new Vector3(localBounds.max.x, 0, localBounds.min.z)  // RR
            };

            bool[] used = new bool[candidates.Length];
            int[] assigned = new int[] { -1, -1, -1, -1 }; // FL, FR, RL, RR

            for (int c = 0; c < 4; c++) {

                float bestDist = float.MaxValue;
                int bestIdx = -1;

                for (int i = 0; i < candidates.Length; i++) {

                    if (used[i]) continue;

                    Vector3 flat = new Vector3(localPositions[i].x, 0, localPositions[i].z);
                    float dist = Vector3.Distance(flat, corners[c]);

                    if (dist < bestDist) {
                        bestDist = dist;
                        bestIdx = i;
                    }

                }

                if (bestIdx >= 0) {
                    assigned[c] = bestIdx;
                    used[bestIdx] = true;
                }

            }

            if (assigned[0] >= 0) result.frontLeft = candidates[assigned[0]];
            if (assigned[1] >= 0) result.frontRight = candidates[assigned[1]];
            if (assigned[2] >= 0) result.rearLeft = candidates[assigned[2]];
            if (assigned[3] >= 0) result.rearRight = candidates[assigned[3]];

            // Collect unassigned.
            List<GameObject> extras = new List<GameObject>();
            for (int i = 0; i < candidates.Length; i++) {
                if (!used[i]) extras.Add(candidates[i]);
            }
            result.unassigned = extras.ToArray();

        }

        return result;

    }

    // Minimum thickness along the thinnest mesh axis for a candidate to qualify as a wheel.
    // Rejects flat brake discs / decals that share a cylindrical profile with real tires but
    // can't serve as wheel models. Modular wheel FBXs (tire + rim + disc as siblings) are the
    // motivating case.
    private const float MIN_WHEEL_THICKNESS = 0.03f;

    public static GameObject[] DetectPossibleAllWheels(GameObject vehicle) {

        List<GameObject> allWheels = new List<GameObject>();
        MeshFilter[] meshFilter = vehicle.GetComponentsInChildren<MeshFilter>();

        if (meshFilter != null) {

            for (int i = 0; i < meshFilter.Length; i++) {

                if (meshFilter[i].sharedMesh != null) {

                    Bounds bounds = meshFilter[i].sharedMesh.bounds;

                    float depth = bounds.size.x;
                    float height = bounds.size.y;
                    float width = bounds.size.z;

                    // Simple cylindrical detection based on aspect ratios
                    if (Mathf.Abs(width - height) < 0.05f && depth < width) {

                        float smallest = Mathf.Min(bounds.size.x, Mathf.Min(bounds.size.y, bounds.size.z));
                        if (smallest < MIN_WHEEL_THICKNESS)
                            continue;

                        allWheels.Add(meshFilter[i].gameObject);

                    }

                }

            }

        }

        return allWheels.ToArray();

    }

    public static GameObject[] DetectPossibleFrontWheels(GameObject vehicle) {

        GameObject[] allWheels = DetectPossibleAllWheels(vehicle);
        List<GameObject> frontWheels = new List<GameObject>();

        for (int i = 0; i < allWheels.Length; i++) {

            if (IsInFront(vehicle, allWheels[i]))
                frontWheels.Add(allWheels[i]);

        }

        return frontWheels.ToArray();

    }

    public static GameObject[] DetectPossibleRearWheels(GameObject vehicle) {

        GameObject[] allWheels = DetectPossibleAllWheels(vehicle);
        List<GameObject> rearWheels = new List<GameObject>();

        for (int i = 0; i < allWheels.Length; i++) {

            if (!IsInFront(vehicle, allWheels[i]))
                rearWheels.Add(allWheels[i]);

        }

        return rearWheels.ToArray();

    }

    private static bool IsInFront(GameObject vehicle, GameObject wheel) {

        // Get the forward direction of the parent
        Vector3 parentForward = vehicle.transform.forward;

        // Get the direction from the parent to the child
        Vector3 directionToChild = wheel.transform.position - vehicle.transform.position;

        // Calculate the dot product
        float dotProduct = Vector3.Dot(parentForward, directionToChild);

        // If the dot product is positive, the child is in front
        return dotProduct > 0;

    }

}
#endif
