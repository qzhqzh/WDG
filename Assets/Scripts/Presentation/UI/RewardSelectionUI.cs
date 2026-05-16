using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WDG
{
    public class RewardSelectionUI : MonoBehaviour
    {
        [SerializeField] private GameObject rewardCardPrefab;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject panelRoot;

        private GameBootstrap _bootstrap;
        private List<GameObject> _cardObjects = new List<GameObject>();
        private List<RewardCard> _currentCards;

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void Start()
        {
            _bootstrap = FindObjectOfType<GameBootstrap>();
            HidePanel();
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.RewardSelection)
            {
                ShowPanel();
            }
            else
            {
                HidePanel();
            }
        }

        private void ShowPanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            // Draw rewards from RewardSystem
            var rewardSystem = ServiceLocator.Get<RewardSystem>();
            _currentCards = rewardSystem.DrawRewards(3);
            ShowRewards(_currentCards);
        }

        private void HidePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            ClearCards();
        }

        public void ShowRewards(List<RewardCard> cards)
        {
            ClearCards();

            foreach (var card in cards)
            {
                CreateCardUI(card);
            }
        }

        private void CreateCardUI(RewardCard card)
        {
            Transform parent = cardContainer != null ? cardContainer : transform;
            GameObject cardGO;

            if (rewardCardPrefab != null)
            {
                cardGO = Instantiate(rewardCardPrefab, parent);
            }
            else
            {
                cardGO = new GameObject($"Card_{card.Id}");
                cardGO.transform.SetParent(parent, false);
                cardGO.AddComponent<RectTransform>();
                cardGO.AddComponent<Button>();
                var textGO = new GameObject("Text");
                textGO.transform.SetParent(cardGO.transform, false);
                textGO.AddComponent<RectTransform>();
                textGO.AddComponent<Text>();
            }

            var label = cardGO.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = $"{card.DisplayName}\n{card.Description}\n[{card.Type}]";
            }

            var button = cardGO.GetComponent<Button>();
            if (button != null)
            {
                var selectedCard = card;
                button.onClick.AddListener(() => OnCardClicked(selectedCard));
            }

            _cardObjects.Add(cardGO);
        }

        private void OnCardClicked(RewardCard card)
        {
            if (_bootstrap != null)
            {
                _bootstrap.OnRewardSelected(card);
            }
            HidePanel();
        }

        private void ClearCards()
        {
            foreach (var go in _cardObjects)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            _cardObjects.Clear();
        }
    }
}
