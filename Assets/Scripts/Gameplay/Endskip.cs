using UnityEngine;

namespace LeafGame
{
    public class FinalSequenceDebug : MonoBehaviour
    {
        public GameFlow gameFlow;

        private void Start()
        {
            gameFlow.BeginFinal();
        }
    }
}