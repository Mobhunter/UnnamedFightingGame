using NUnit.Framework;
using UnityEngine;

public class ContactCheck : MonoBehaviour
{
    public bool HasContact;
    public void OnTriggerEnter2D(Collider2D collision)
    {
        HasContact = true;
    }
    public void OnTriggerExit2D(Collider2D collision)
    {
        HasContact = false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = (HasContact) ? Color.green : Color.red;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }
}

public class HitBoxCheck : MonoBehaviour
{
    public bool HasContact;

    private void Reset()
    {
        // Ensure BoxCollider2D exists and is set as trigger
        var box = GetComponent<BoxCollider2D>();
        if (box == null)
            box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Only activate if the collider is tagged as "Attack"
        if (collision.CompareTag("Attack"))
        {
            HasContact = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Only deactivate if the collider is tagged as "Attack"
        if (collision.CompareTag("Attack"))
        {
            HasContact = false;
        }
    }

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = HasContact ? Color.yellow : Color.gray;
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
    }
}
