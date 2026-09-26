using UnityEngine;

namespace LeafGame
{
    public enum WindResult { Perfect, Good, Miss }
    public enum WindDirection { Left = -1, Right = 1 }

    public static class WindTimingEvaluator
    {
        public static WindResult Evaluate(float error, float perfect, float good)
        {
            float distance = Mathf.Abs(error);
            return distance <= perfect ? WindResult.Perfect : distance <= good ? WindResult.Good : WindResult.Miss;
        }
    }
}
