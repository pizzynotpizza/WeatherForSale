using System;
using UnityEngine;

namespace WeatherForSale.Shop
{
    /// <summary>
    /// The ordered list of customers served in one shop day. Index 0 arrives first.
    /// Implements: story-002 criteria 2 and 7 ("the next customer comes").
    /// </summary>
    [CreateAssetMenu(fileName = "CustomerRoster", menuName = "Weather For Sale/Shop/Customer Roster")]
    public sealed class CustomerRoster : ScriptableObject
    {
        [Tooltip("Customers in arrival order.")]
        public CustomerData[] customers = Array.Empty<CustomerData>();

        /// <summary>Number of customers in the day.</summary>
        public int Count => customers != null ? customers.Length : 0;

        /// <summary>Returns the customer at <paramref name="index"/>, or null when out of range.</summary>
        public CustomerData Get(int index)
        {
            if (customers == null || index < 0 || index >= customers.Length) return null;
            return customers[index];
        }
    }
}
