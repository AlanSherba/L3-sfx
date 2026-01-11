using UnityEngine;
using System.Collections.Generic;

public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Bootstrap()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("L3 SFX Manager");
            Instance = obj.AddComponent<SfxManager>();
            DontDestroyOnLoad(obj);
            Instance.Initialize();
        }
    }

    private const int INITIAL_POOL_SIZE = 16;
    private const int MAX_POOL_SIZE = 64;

    private Queue<SfxPlayer> availablePlayers;
    private List<SfxPlayer> activePlayers;
    private Transform poolParent;

    private void Initialize()
    {
        availablePlayers = new Queue<SfxPlayer>();
        activePlayers = new List<SfxPlayer>();

        poolParent = new GameObject("SfxPool").transform;
        poolParent.SetParent(transform);

        for (int i = 0; i < INITIAL_POOL_SIZE; i++)
        {
            CreatePooledPlayer();
        }
    }

    public static SfxPlayer Play(Sfx sfx)
    {
        return Play(sfx, Vector3.zero);
    }

    public static SfxPlayer Play(Sfx sfx, Vector3 position)
    {
        if (Instance == null || sfx == null || sfx.clips == null || sfx.clips.Count == 0)
            return null;

        return Instance.PlayInternal(sfx, position);
    }

    private SfxPlayer PlayInternal(Sfx sfx, Vector3 position)
    {
        SfxPlayer player = GetPlayerFromPool();
        player.transform.position = position;
        player.Play(sfx);
        activePlayers.Add(player);
        return player;
    }

    private SfxPlayer GetPlayerFromPool()
    {
        if (availablePlayers.Count > 0)
        {
            SfxPlayer player = availablePlayers.Dequeue();
            player.gameObject.SetActive(true);
            return player;
        }

        if (activePlayers.Count < MAX_POOL_SIZE)
        {
            SfxPlayer player = CreatePooledPlayer();
            player.gameObject.SetActive(true);
            return player;
        }

        SfxPlayer oldest = activePlayers[0];
        activePlayers.RemoveAt(0);
        oldest.Stop();
        return oldest;
    }

    private SfxPlayer CreatePooledPlayer()
    {
        GameObject obj = new GameObject("SfxPlayer");
        obj.transform.SetParent(poolParent);

        AudioSource audioSource = obj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        SfxPlayer player = obj.AddComponent<SfxPlayer>();
        player.Initialize(this, audioSource);

        obj.SetActive(false);
        availablePlayers.Enqueue(player);
        return player;
    }

    internal void ReturnToPool(SfxPlayer player)
    {
        activePlayers.Remove(player);
        player.gameObject.SetActive(false);
        availablePlayers.Enqueue(player);
    }
}
