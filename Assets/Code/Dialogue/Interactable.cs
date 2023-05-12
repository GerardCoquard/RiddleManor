﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour
{
    public Transform iconDisplayPos;
    public Transform textDisplayPos;
    public DialogueNode startNode;
    private bool playerIn;

    private void Update()
    {
        
        if (PlayerController.instance.CanInteract() && playerIn)
        {
            WorldScreenUI.instance.SetIcon(IconType.Dialogue, iconDisplayPos.position);
        }
        else
        {
            WorldScreenUI.instance.HideIcon(IconType.Dialogue);
        }
    }

    private void DialogueInteract(InputAction.CallbackContext context)
    {
        if (context.performed && PlayerController.instance.CanInteract())
        {
            WorldScreenUI.instance.SetDialogue(startNode, textDisplayPos);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            InputManager.GetAction("Push").action += DialogueInteract;
            playerIn = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            InputManager.GetAction("Push").action -= DialogueInteract;
            playerIn=false;
        }
    }

}





