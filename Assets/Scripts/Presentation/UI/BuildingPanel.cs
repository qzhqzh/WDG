using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WDG
{
    public class BuildingPanel : MonoBehaviour
    {
        [SerializeField] private GameObject buildingButtonPrefab;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject panelRoot;

        private GameBootstrap _bootstrap;
        private List<BuildingButtonData> _buttons = new List<BuildingButtonData>();

        private class BuildingButtonData
        {
            public string ConfigId;
            public int Cost;
            public Button Button;
            public Text Label;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void Start()
        {
            _bootstrap = FindObjectOfType<GameBootstrap>();
            RefreshBuildingList();
            UpdateVisibility();
        }

        public void RefreshBuildingList()
        {
            // Clear existing buttons
            foreach (var data in _buttons)
            {
                if (data.Button != null)
                {
                    Destroy(data.Button.gameObject);
                }
            }
            _buttons.Clear();

            var buildingSystem = ServiceLocator.Get<BuildingSystem>();
            var available = buildingSystem.GetAvailableBuildings();

            foreach (var config in available)
            {
                CreateBuildingButton(config);
            }

            UpdateButtonStates();
        }

        private void CreateBuildingButton(BuildingConfig config)
        {
            Transform parent = buttonContainer != null ? buttonContainer : transform;
            GameObject buttonGO;

            if (buildingButtonPrefab != null)
            {
                buttonGO = Instantiate(buildingButtonPrefab, parent);
            }
            else
            {
                buttonGO = new GameObject($"Btn_{config.buildingId}");
                buttonGO.transform.SetParent(parent, false);
                buttonGO.AddComponent<RectTransform>();
                buttonGO.AddComponent<Button>();
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(buttonGO.transform, false);
                textGO.AddComponent<RectTransform>();
                textGO.AddComponent<Text>();
            }

            var button = buttonGO.GetComponent<Button>();
            var label = buttonGO.GetComponentInChildren<Text>();

            if (label != null)
            {
                label.text = $"{config.displayName}\n({config.cost}g)";
            }

            var data = new BuildingButtonData
            {
                ConfigId = config.buildingId,
                Cost = config.cost,
                Button = button,
                Label = label
            };

            string configId = config.buildingId;
            if (button != null)
            {
                button.onClick.AddListener(() => OnBuildingButtonClicked(configId));
            }

            _buttons.Add(data);
        }

        private void OnBuildingButtonClicked(string configId)
        {
            if (_bootstrap != null)
            {
                _bootstrap.OnBuildingSelected(configId);
            }
        }

        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            UpdateButtonStates(evt.NewAmount);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            UpdateVisibility(evt.NewState);
        }

        private void UpdateButtonStates(int currentGold = -1)
        {
            if (currentGold < 0)
            {
                var resourceSystem = ServiceLocator.Get<ResourceSystem>();
                currentGold = resourceSystem != null ? resourceSystem.CurrentGold : 0;
            }

            foreach (var data in _buttons)
            {
                if (data.Button != null)
                {
                    data.Button.interactable = currentGold >= data.Cost;
                }
            }
        }

        private void UpdateVisibility(GameState state = GameState.Initializing)
        {
            if (panelRoot == null)
                return;

            if (state == GameState.Initializing)
            {
                var gameCore = ServiceLocator.Get<GameCore>();
                if (gameCore != null)
                {
                    state = gameCore.CurrentState;
                }
            }

            panelRoot.SetActive(state == GameState.Preparation);
        }
    }
}
