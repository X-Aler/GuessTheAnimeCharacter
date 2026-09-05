using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX")]
    [Tooltip("Единый источник для коротких звуков (клики)")]
    [SerializeField] private AudioSource sfxSource;

    private Dictionary<AudioClip, AudioSource> loopingSources = new Dictionary<AudioClip, AudioSource>();

    [SerializeField] public AudioClip buttonSound;
    [SerializeField] public AudioClip buttonEnterSound;
    [SerializeField] public AudioClip correctSound;
    [SerializeField] public AudioClip errorSound;
    [SerializeField] public AudioClip OST;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        PlayLoopingSound(OST, 0.15f);
    }

    /// <summary>
    /// Проигрывает короткий звук поверх остальных (клики мышкой, эффекты)
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Включает зацикленный звук (фоновая музыка или долгий процесс шредера)
    /// </summary>
    public void PlayLoopingSound(AudioClip clip, float volume)
    {
        if (clip == null) return;

        // Если этот конкретный звук уже играет, не запускаем дубликат
        if (loopingSources.ContainsKey(clip)) return;

        // Создаем новый AudioSource прямо на объекте AudioManager
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.clip = clip;
        newSource.loop = true;
        newSource.volume = volume;
        newSource.Play();

        // Записываем в словарь, чтобы потом знать, как его остановить
        loopingSources.Add(clip, newSource);
    }

    /// <summary>
    /// Останавливает ТОЛЬКО переданный зацикленный звук
    /// </summary>
    public void StopLoopingSound(AudioClip clip)
    {
        if (clip == null) return;

        // Проверяем, играет ли сейчас этот звук
        if (loopingSources.ContainsKey(clip))
        {
            AudioSource sourceToStop = loopingSources[clip];

            sourceToStop.Stop();

            // Удаляем компонент, чтобы на объекте не скапливались пустые AudioSource
            Destroy(sourceToStop);

            // Убираем запись из словаря
            loopingSources.Remove(clip);
        }
    }
}