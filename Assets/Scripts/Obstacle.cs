using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public abstract class Obstacle : Interactable
{
    [SerializeField] int damage = 1;

    bool consumed;

    public override void OnPlayerContact(PlayerController player)
    {
        if (consumed) return;
        consumed = true;
        player.TakeDamage(damage);
    }

    protected void FitCollider(Sprite sprite)
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.size = new Vector2(sprite.bounds.size.x * 0.7f, sprite.bounds.size.y * 0.85f);
        box.offset = sprite.bounds.center;
    }
}
