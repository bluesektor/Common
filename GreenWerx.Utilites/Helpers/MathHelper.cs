// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using GreenWerx.Models.Finance;

namespace GreenWerx.Utilites.Helpers
{
    public class MathHelper
    {
        protected MathHelper()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="operand">This is the Price field for MileRangePrice, and the Premium field for LoadCountPremiums</param>
        /// <param name="calcOperator" =,+,-,*,/,% operations are supported</param>
        /// <returns></returns>
        public static decimal ApplyCalculation(decimal amount, string calcOperator, decimal operand)
        {
            decimal res = 0;
            if (string.IsNullOrEmpty(calcOperator))
                return 0;

            calcOperator = calcOperator.ToLower();

            switch (calcOperator)
            {
                case "*":
                    res = amount * operand;
                    break;

                case "%":
                    if (operand == 0)
                    {
                        res = 0;
                    }
                    else
                    {
                        decimal pct = operand / 100;
                        var pctChange = amount * pct;
                        res = amount + pctChange;
                    }
                    break;

                case "=":
                    res = amount;
                    break;

                case "+":
                    res = amount + operand;
                    break;

                case "-":
                    res = amount - operand;
                    break;

                case "/":
                    if (amount == 0 || operand == 0)
                    {
                        Debug.Assert(false, "Cannot divide by zero!");
                        res = 0;
                    }
                    else
                        res = amount / operand;
                    break;

                default:
                    Debug.Assert(false, "FORGOT THE OPERATOR!");
                    break;
            }
            return res;
        }

        public static decimal CalcAdjustment(decimal ammount, ref List<PriceRuleLog> priceRules)
        {
            decimal res = 0;

            for (int i = 0; i < priceRules.Count; i++)
            {
                decimal total = ApplyCalculation(ammount, priceRules[i].Operator, priceRules[i].Operand);

                if (total > priceRules[i].Maximum)
                {
                    total = priceRules[i].Maximum;
                    priceRules[i].CalcDetail += "Maximum Threshhold applied:" + total.ToString() + Environment.NewLine;
                }

                if (total < priceRules[i].Minimum)
                {
                    total = priceRules[i].Minimum;
                    priceRules[i].CalcDetail += "Minimum Threshhold applied:" + total.ToString() + Environment.NewLine;
                }

                priceRules[i].Result = res;

                res += total;
            }
            return res;
        }

        public static double degreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double Distance(double lat1, double lon1, double lat2, double lon2, bool returnMiles = true)
        {
            int earthRadiusKm = 6371;
            int earthRadiusMiles = 3959;
            double dLat = degreesToRadians(lat2 - lat1);
            double dLon = degreesToRadians(lon2 - lon1);

            lat1 = degreesToRadians(lat1);
            lat2 = degreesToRadians(lat2);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            if (returnMiles == true)
            {
                return earthRadiusMiles * c;
            }
            else
            {
                return earthRadiusKm * c;
            }

            //double startCosLatitude = Math.Cos(startLatitude);
            //double endCosLatitude = Math.Cos(endLatitude);

            //double dLat = endLatitude - startLatitude;
            //double dLon = endLongitude - startLongitude;

            //double a = Math.Pow(Math.Sin(dLat / 2.0), 2) +
            //        startCosLatitude *
            //        endCosLatitude *
            //        Math.Pow(Math.Sin(dLon / 2.0), 2.0);

            //double c = 2 * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));
            //double d = (returnMiles ? 3956 : 6367) * c;

            //return d;
        }
    }
}