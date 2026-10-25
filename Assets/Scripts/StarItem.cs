using UnityEngine;

public class StarItem : Interactable
{
    [SerializeField] int points = 50;

    public override void OnPlayerContact(PlayerController player)
    {
        GameManager.Instance.AddScore(points);
        Destroy(gameObject);
    }
}
