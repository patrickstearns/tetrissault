using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour, GameControls.IMenuActions {

    private GameControls controls;

    public TextMeshProUGUI[] ButtonLabels;

    private int focusedIndex;

    public void Show() {
        gameObject.SetActive(true);

        focusedIndex = 0;
        refresh();

        if (controls == null){
            controls = new GameControls();
            controls.Menu.SetCallbacks(this);
        }
        controls.Menu.Enable();
    }

    private void refresh() {
        for (int i = 0; i < ButtonLabels.Length; i++) {
            ButtonLabels[i].color = i == focusedIndex ? Color.yellow : Color.white;
        }
    }

    public void OnMove(InputAction.CallbackContext context) {
        if (context.performed) {
            Vector2 value = context.ReadValue<Vector2>();

            if (value.y > 0) focusedIndex--;
            else if (value.y < 0) focusedIndex++;

            if (focusedIndex >= ButtonLabels.Length) focusedIndex = 0;
            if (focusedIndex  < 0) focusedIndex = ButtonLabels.Length - 1;

            refresh();

            AudioManager.Instance.PlaySFX(AudioManager.Instance.move);
        }
    }

    public void OnConfirm(InputAction.CallbackContext context) {
        if (!context.performed) return;

        AudioManager.Instance.PlaySFX(AudioManager.Instance.select);

        if (focusedIndex == 0) {
            controls.Menu.Disable();
            SceneManager.LoadScene("TitleScene");
            gameObject.SetActive(false);
        }

    }

}
