using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GUIPack2DFree
{
    public abstract class Popup : MonoBehaviour
    {
        protected PopupSystem popupSystem;

        public UnityEvent onOpen;
        public UnityEvent onClose;

        private Animator animator;

        protected virtual void Awake()
        {
            animator = GetComponent<Animator>();
        }

        protected virtual void Start()
        {
            animator.Play("Open");
            onOpen.Invoke();
        }

        public virtual void Initialize(PopupSystem popups)
        {
            popupSystem = popups;
        }

        public void Close()
        {
            onClose.Invoke();
            AudioManager.Instance.PlayEffects(AudioManager.Instance.buttonClick);

            if (popupSystem != null)
            {
                popupSystem.ClosePopup();
            }

            if (animator != null)
            {
                animator.Play("Close");
                StartCoroutine(DestroyPopup());
            }
            else
            {
                Destroy(gameObject);
            }
        }

        protected virtual IEnumerator DestroyPopup()
        {
            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }
    }
}
