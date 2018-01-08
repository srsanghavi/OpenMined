﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using OpenMined.Network.Utils;

namespace OpenMined.Network.Servers
{
    public class Request
    {
        public class EthResponse
        {
            public string jsonrpc;
            public int id;
        }

        public class BlockNumber : EthResponse
        {
            public string result;
        }

        public class Call : EthResponse
        {
            public string result;
        }

        public static string contractAddress = "0xd60e1a150b59a89a8e6e6ff2c03ffb6cb4096205";
        public static string infuraURL = "https://api.infura.io/v1/jsonrpc/";
        public static string infuraNetwork = "rinkeby/";

        public Coroutine coroutine { get; private set; }
        public object result;
        private IEnumerator target;

        public Request(MonoBehaviour owner, IEnumerator target)
        {
            this.target = target;
            this.coroutine = owner.StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            while (target.MoveNext())
            {
                result = target.Current;
                yield return result;
            }
        }

        public static IEnumerator Get<T>(string method, string data = "") where T : EthResponse
        {
            string URL = infuraURL + infuraNetwork + method;

            if (data != "") {
                URL += "?params=" + data;
            }

            Debug.LogFormat("Request.Get {0}", URL);
            UnityWebRequest www = UnityWebRequest.Get(URL);
            www.SetRequestHeader("accept", "application/json");

            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                yield return null;
            }
            else
            {
                string json = www.downloadHandler.text;

                T response = JsonUtility.FromJson<T>(json);
                yield return response;
            }
        }

        public static IEnumerator GetBlockNumber(MonoBehaviour owner)
        {
            Request req = new Request(owner, Request.Get<Request.BlockNumber>("eth_blockNumber"));
            yield return req.coroutine;

            Request.BlockNumber response = req.result as Request.BlockNumber;
            int result = (int) new System.ComponentModel.Int32Converter().ConvertFromString(response.result);
            Debug.LogFormat("\nCurrent Rinkeby Block Number: {0}", result.ToString("N"));
        }

        private static string encodeData(string data)
        {
            string encodedData = WWW.EscapeURL("[{\"to\":\"" + contractAddress + "\",\"data\":\"" + data + "\"},\"latest\"]");
            return encodedData;
        }

        public static IEnumerator GetNumModels(MonoBehaviour owner)
        {
            // TODO: convert "getNumModels" to hex.
            string data = encodeData("0x3c320cc2");

            Request req = new Request(owner, Request.Get<Request.Call>("eth_call", data));
            yield return req.coroutine;

            Request.Call response = req.result as Request.Call;
            int result = (int)new System.ComponentModel.Int32Converter().ConvertFromString(response.result);
            Debug.LogFormat("\nNum Models: {0}", result.ToString("N"));
        }

        public static IEnumerator GetModel(MonoBehaviour owner, int modelId)
        {
            /*var keccak = new Sha3Keccack();

            var d = keccak.CalculateHash("getModel(uint256)");

            Debug.Log(d);*/
            
            // TODO: convert "getModel" and modelId to hex.
            string data = encodeData("0x6d3616940000000000000000000000000000000000000000000000000000000000000001");

            Request req = new Request(owner, Request.Get<Request.Call>("eth_call", data));
            yield return req.coroutine;

            Request.Call response = req.result as Request.Call;
            Debug.LogFormat("Model 1 {0}", response.result);

            var address = Request.GetAddress(response.result);
            var bounty = Request.GetBounty(response.result);
            var initialError = Request.GetInitialError(response.result);
            var targetError = Request.GetTargetError(response.result);

            Debug.LogFormat("address {0}, bounty {1}, initialError {2}, targetError {3}", address, bounty, initialError, targetError);
            
            //var addresses = Request.getWeightAddresses();
        }

        public static string GetAddress(string hexString)
        {
            var parameter = Request.GetParameter(hexString, 0);

            return parameter;
        }

        public static uint GetBounty(string hexString)
        {
            var parameter = Request.GetParameter(hexString, 1);
            Debug.Log(parameter);

            uint decval = System.Convert.ToUInt32(parameter, 16);

            return decval;
        }

        public static uint GetInitialError(string hexString)
        {
            var parameter = Request.GetParameter(hexString, 2);
            Debug.Log(parameter);

            uint decval = System.Convert.ToUInt32(parameter, 16);

            return decval;
        }

        public static uint GetTargetError(string hexString)
        {
            var parameter = Request.GetParameter(hexString, 3);
            Debug.Log(parameter);

            uint decval = System.Convert.ToUInt32(parameter, 16);

            return decval;
        }

        public static string GetParameter(string hexString, int parameter)
        {
            var hs = hexString.Substring(64 * parameter + 2, 64);
            Debug.LogFormat("bleh {0}, {1}", 64 * parameter + 2, 64 * parameter + 64 + 2);
            Debug.Log(hs);

            hs = Request.StripPadding(hs);

            return hs;
        }

        public static string StripPadding(string hexString)
        {
            var index = 0;
            for (int j = 0; j < hexString.Length; j++)
            {
                if (hexString[j] != '0')
                {
                    index = j;
                    break;
                }
            }
                        
            return hexString.Remove(0, index);
        }
    }
}
