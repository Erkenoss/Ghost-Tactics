using UnityEngine;
using System.Collections.Generic;

namespace Crimson.Setting
{
    [CreateAssetMenu(fileName = "Container Setting SO", menuName = "Crimson/Setting/SO Container")]
    public class ContainerSettingSO : ScriptableObject
    {
        public List<SettingSO> Settings { get { return settings; } set { settings = value; } }
 
        /// <summary>
        /// List of all setting in the game
        /// </summary>
        [SerializeField]
        private List<SettingSO> settings = new List<SettingSO>(); 
    }
}