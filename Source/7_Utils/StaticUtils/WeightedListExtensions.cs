using System;
using UnityEngine;

namespace EasyOffset {
    public static class WeightedListExtensions {
        #region Average

        public static Vector3 GetAverage(this WeightedList<Vector3> list) {
            var result = Vector3.zero;
            var totalWeight = 0.0f;

            foreach (var entry in list.Entries) {
                totalWeight += entry.Weight;
                result += entry.Value * entry.Weight;
            }

            return totalWeight > 0 ? result / totalWeight : Vector3.zero;
        }

        #endregion

        #region Deviation

        public static float GetDeviationFromPoint(this WeightedList<Vector3> list, Vector3 point) {
            if (list.Entries.Count == 0) throw new InvalidOperationException("Sequence contains no elements");
            var sum = 0f;
            foreach (var entry in list.Entries) {
                sum += (entry.Value - point).sqrMagnitude;
            }

            return Mathf.Sqrt(sum / list.Entries.Count);
        }

        public static float GetDeviationFromPlane(this WeightedList<Vector3> list, Plane plane) {
            if (list.Entries.Count == 0) throw new InvalidOperationException("Sequence contains no elements");
            var sum = 0f;
            foreach (var entry in list.Entries) {
                var distance = plane.GetDistanceToPoint(entry.Value);
                sum += distance * distance;
            }

            return Mathf.Sqrt(sum / list.Entries.Count);
        }

        #endregion

        #region CalculatePlane

        public static Plane CalculatePlane(this WeightedList<Vector3> list, Vector3 initialNormal) {
            var averagePoint = list.GetAverage();

            var covariance = Matrix4x4.zero;
            foreach (var entry in list.Entries) {
                var relative3 = entry.Value - averagePoint;
                var relative4 = new Vector4(relative3.x, relative3.y, relative3.z, 1);
                covariance = MathUtils.MatrixSum(covariance, MathUtils.GetOuterProduct(relative4, relative4));
            }

            var convergenceMatrix = covariance.inverse;

            Vector4 normal = initialNormal;

            for (var i = 0; i < 20; i++) {
                normal = convergenceMatrix * normal;
                normal = normal.normalized;
            }

            return new Plane(normal, averagePoint);
        }

        #endregion

        #region GetMinMaxAngles

        public static void GetSwingAngles(
            this WeightedList<Vector3> list,
            Vector3 origin,
            Quaternion rotation,
            out float minAngle,
            out float maxAngle
        ) {
            var inverseRotation = Quaternion.Inverse(rotation);
            var entries = list.Entries;
            if (entries.Count == 0) throw new InvalidOperationException("Sequence contains no elements");

            float AngleAt(int index) {
                var localPosition = inverseRotation * (entries[index].Value - origin);
                return Mathf.Atan2(localPosition.y, localPosition.x);
            }

            var firstAngle = AngleAt(0);
            var absoluteMinimumAngle = firstAngle;
            var absoluteMaximumAngle = firstAngle;
            for (var i = 1; i < entries.Count; i++) {
                var angle = AngleAt(i);
                if (angle < absoluteMinimumAngle) absoluteMinimumAngle = angle;
                if (angle > absoluteMaximumAngle) absoluteMaximumAngle = angle;
            }

            var margin = Mathf.Abs(absoluteMaximumAngle - absoluteMinimumAngle) / 6;
            var previousAngle = firstAngle;
            var previousDirectionPositive = false;
            var hasPreviousDirection = false;
            var localMinimumCount = 0;
            var localMaximumCount = 0;
            var minimumSum = 0f;
            var maximumSum = 0f;
            var minimumSamples = 0;
            var maximumSamples = 0;

            for (var i = 1; i < entries.Count; i++) {
                var angle = AngleAt(i);
                var directionPositive = angle - previousAngle >= 0;
                if (hasPreviousDirection && directionPositive != previousDirectionPositive) {
                    if (previousDirectionPositive) {
                        localMaximumCount++;
                        if (!(Mathf.Abs(previousAngle - absoluteMaximumAngle) > margin)) {
                            maximumSum += previousAngle;
                            maximumSamples++;
                        }
                    } else {
                        localMinimumCount++;
                        if (!(Mathf.Abs(previousAngle - absoluteMinimumAngle) > margin)) {
                            minimumSum += previousAngle;
                            minimumSamples++;
                        }
                    }
                }

                hasPreviousDirection = true;
                previousDirectionPositive = directionPositive;
                previousAngle = angle;
            }

            if (localMinimumCount == 0 && !(Mathf.Abs(firstAngle - absoluteMinimumAngle) > margin)) {
                minimumSum += firstAngle;
                minimumSamples++;
            }
            if (localMaximumCount == 0 && !(Mathf.Abs(firstAngle - absoluteMaximumAngle) > margin)) {
                maximumSum += firstAngle;
                maximumSamples++;
            }
            if (previousAngle <= absoluteMinimumAngle) {
                minimumSum += previousAngle;
                minimumSamples++;
            }
            if (previousAngle >= absoluteMaximumAngle) {
                maximumSum += previousAngle;
                maximumSamples++;
            }

            minAngle = minimumSamples > 0 ? minimumSum / minimumSamples : 0f;
            maxAngle = maximumSamples > 0 ? maximumSum / maximumSamples : 0f;
        }

        #endregion
    }
}
