using UnityEngine;
using System.Collections;

namespace Game.PlayerGuide.Views
{
    public class FeifeiCharacterView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject feifeiCharacter;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform characterTransform;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Coroutine currentMoveCoroutine;

        private void Awake()
        {
            if (feifeiCharacter == null)
                feifeiCharacter = gameObject;

            if (characterTransform == null)
                characterTransform = transform;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        public void SetAnimatorSpeed(float speed)
        {
            if (animator != null)
            {
                animator.speed = speed;
            }
        }

        public void PlayAnimation(string stateName, int layer = 0)
        {
            if (animator != null && !string.IsNullOrEmpty(stateName))
            {
                animator.Play(stateName, layer);
            }
        }

        public void SetBool(string parameterName, bool value)
        {
            if (animator != null && !string.IsNullOrEmpty(parameterName))
            {
                if (HasParameter(parameterName, AnimatorControllerParameterType.Bool))
                {
                    animator.SetBool(parameterName, value);
                }
            }
        }

        public void SetTrigger(string parameterName)
        {
            if (animator != null && !string.IsNullOrEmpty(parameterName))
            {
                if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
                {
                    animator.SetTrigger(parameterName);
                }
            }
        }

        public void SetFloat(string parameterName, float value)
        {
            if (animator != null && !string.IsNullOrEmpty(parameterName))
            {
                if (HasParameter(parameterName, AnimatorControllerParameterType.Float))
                {
                    animator.SetFloat(parameterName, value);
                }
            }
        }

        public void SetInteger(string parameterName, int value)
        {
            if (animator != null && !string.IsNullOrEmpty(parameterName))
            {
                if (HasParameter(parameterName, AnimatorControllerParameterType.Int))
                {
                    animator.SetInteger(parameterName, value);
                }
            }
        }

        public void MoveTo(Vector3 targetPosition, float duration = -1f)
        {
            if (currentMoveCoroutine != null)
            {
                StopCoroutine(currentMoveCoroutine);
            }

            float actualDuration = duration > 0 ? duration : Vector3.Distance(characterTransform.position, targetPosition) / moveSpeed;
            currentMoveCoroutine = StartCoroutine(MoveToCoroutine(targetPosition, actualDuration));
        }

        public void StopMovement()
        {
            if (currentMoveCoroutine != null)
            {
                StopCoroutine(currentMoveCoroutine);
                currentMoveCoroutine = null;
            }
        }

        public void SetPosition(Vector3 position)
        {
            StopMovement();
            if (characterTransform != null)
            {
                characterTransform.position = position;
            }
        }

        public void SetActive(bool active)
        {
            if (feifeiCharacter != null)
            {
                feifeiCharacter.SetActive(active);
            }
        }

        public void FlipCharacter(bool faceRight)
        {
            if (characterTransform != null)
            {
                Vector3 scale = characterTransform.localScale;
                scale.x = faceRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                characterTransform.localScale = scale;
            }
        }

        private IEnumerator MoveToCoroutine(Vector3 targetPosition, float duration)
        {
            Vector3 startPosition = characterTransform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = movementCurve.Evaluate(t);
                characterTransform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
                yield return null;
            }

            characterTransform.position = targetPosition;
            currentMoveCoroutine = null;
        }

        private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == parameterName && param.type == type)
                    return true;
            }

            return false;
        }

        public Vector3 GetPosition()
        {
            return characterTransform != null ? characterTransform.position : Vector3.zero;
        }

        public bool IsMoving()
        {
            return currentMoveCoroutine != null;
        }
    }
}
