using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;

namespace Prototype.NetworkLobby
{
    [Serializable]
    public class ServerList
    {
        public string[] list;
    }
    public class LobbyServerList : MonoBehaviour
    {
        public LobbyManager lobbyManager;

        public RectTransform serverListRect;
        public GameObject serverEntryPrefab;
        public GameObject noServerFound;

        public string serversRegistryUrl;

        protected int currentPage = 0;
        protected int previousPage = 0;

        static Color OddServerColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        static Color EvenServerColor = new Color(.94f, .94f, .94f, 1.0f);

        void OnEnable()
        {
            currentPage = 0;
            previousPage = 0;

            foreach (Transform t in serverListRect)
                Destroy(t.gameObject);

            noServerFound.SetActive(false);

            if (string.IsNullOrEmpty(serversRegistryUrl))
            {
                RequestPage(0);
            }
            else
            {
                StartCoroutine(GetServerRegistry());
            }
        }

        public IEnumerator GetServerRegistry()
        {
            var req = UnityWebRequest.Get(serversRegistryUrl);
            yield return req.Send();
            var serverList = JsonUtility.FromJson<ServerList>(req.downloadHandler.text).list.Select(addr => {
                var endpointComponents = addr.Split(':');
                return new IPEndPoint(IPAddress.Parse(endpointComponents[0]), int.Parse(endpointComponents[1]));
            }).ToList();
            int i = 0;
            OnGUIMatchList<IPEndPoint>(!req.isNetworkError && !req.isHttpError, "", serverList, match => {

                GameObject o = Instantiate(serverEntryPrefab) as GameObject;
				o.GetComponent<LobbyServerEntry>().Populate(match, lobbyManager, (i % 2 == 0) ? OddServerColor : EvenServerColor);
                ++i;

                return o;
            });
        }

		public void OnGUIMatchList<T>(bool success, string extendedInfo, List<T> matches, Func<T, GameObject> entryFactory)
		{
			if (matches.Count == 0 || !success)
			{
                if (currentPage == 0)
                {
                    noServerFound.SetActive(true);
                }

                currentPage = previousPage;

                return;
            }

            noServerFound.SetActive(false);
            foreach (Transform t in serverListRect)
                Destroy(t.gameObject);

			for (int i = 0; i < matches.Count; ++i)
			{
                var o = entryFactory(matches[i]);
                o.transform.SetParent(serverListRect, false);
                // GameObject o = Instantiate(serverEntryPrefab) as GameObject;

				// o.GetComponent<LobbyServerEntry>().Populate(matches[i], lobbyManager, (i % 2 == 0) ? OddServerColor : EvenServerColor);

				// o.transform.SetParent(serverListRect, false);
            }
        }

        public void ChangePage(int dir)
        {
            int newPage = Mathf.Max(0, currentPage + dir);

            //if we have no server currently displayed, need we need to refresh page0 first instead of trying to fetch any other page
            if (noServerFound.activeSelf)
                newPage = 0;

            RequestPage(newPage);
        }

        public void RequestPage(int page)
        {
            previousPage = currentPage;
            currentPage = page;
            int i = 0;
            Func<MatchInfoSnapshot, GameObject> factory = match => {

                GameObject o = Instantiate(serverEntryPrefab) as GameObject;
				o.GetComponent<LobbyServerEntry>().Populate(match, lobbyManager, (i % 2 == 0) ? OddServerColor : EvenServerColor);
                ++i;
                return o;
            };
			lobbyManager.matchMaker.ListMatches(page, 6, "", true, 0, 0, (success, extendedInfo, matchList) => OnGUIMatchList(success, extendedInfo, matchList, factory));
		}
    }
}