using UnityEngine;
using UnityEngine.SceneManagement;

public class FixedMusicManager : MonoBehaviour
{
    public AudioClip forestMusic; // 15분 숲 음원
    public AudioClip oceanMusic;  // 15분 바다 음원

    void Start()
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true; // 무한 반복 켜기
        audioSource.playOnAwake = true;

        // 씬 이름에 "2"가 있으면 바다, 아니면 숲 재생
        bool isOcean = SceneManager.GetActiveScene().name.Contains("2");
        audioSource.clip = isOcean ? oceanMusic : forestMusic;
        
        audioSource.Play();
    }
}