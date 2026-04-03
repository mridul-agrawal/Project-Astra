using UnityEngine;

namespace ProjectAstra.Core
{
    public class SceneUIRoot : MonoBehaviour
    {
        [SerializeField] private Transform _buttonContainer;

        public Transform ButtonContainer => _buttonContainer;
    }
}
