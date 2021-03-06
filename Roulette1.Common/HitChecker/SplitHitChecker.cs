﻿using System;
using System.Collections.Generic;

namespace Roulette1
{
    public class SplitHitChecker : HitChecker
    {
        public readonly int HitNumber1;
        public readonly int HitNumber2;
        public readonly bool IsVertical;

        public override BettingType BettingType => BettingType.Split;
        public override int Odds => 17;

        public SplitHitChecker(int num, bool isVertical)
        {
            if(num == 100)
            {
                HitNumber1 = 100;
                HitNumber2 = 10000;
                return;
            }

            IsVertical = isVertical;

            this.HitNumber1 = num;
            this.HitNumber2 = IsVertical ? num + 3 : num + 1;

            this.CheckValidate();
        }

        protected override void CheckValidate()
        {
            int small = Math.Min(this.HitNumber1, this.HitNumber2);
            int big = Math.Max(this.HitNumber1, this.HitNumber2);


            if (IsVertical)
            {
                Street row = small.GetStreet();
                if (DeniedStreets.Contains(row))
                    Throw(small, "허용되지 않는 vertial split 의 row");
            }
            else
            {
                Column col = Number.GetColumn(small);
                if(AllowedColumns.Contains(col) == false)
                {
                    Throw(small, "허용되지 않는 horizonal split 의 column");
                }
            }

            int diff = big - small;

            if (diff != 1 && diff != 3 && diff != 100) //100은 0+00 split
            {
                this.Throw(small, "인접하지 않은 숫자");
            }

            if (Number.IsAtomicNumber(big) == false
                || Number.IsAtomicNumber(small) == false)
                this.Throw(small, "허용되지 않은 숫자");
        }


        public override bool IsHit(int number) => this.HitNumber1 == number || this.HitNumber2 == number;

        public static List<Column> AllowedColumns = new List<Column>() { Column.C1, Column.C2 };
        public static List<Street> DeniedStreets = new List<Street>() { Street.None, Street.OutOfStreet, Street.S34 };

        public static List<HitChecker> Gen()
        {
            List<HitChecker> result = new List<HitChecker>();
            
            foreach (int num in Number.GetAllNumbers())
            {
                Column col = Number.GetColumn(num);
                Street street = num.GetStreet();

                if (Number.Is0(num) || AllowedColumns.Contains(col))
                {
                    var horizontalHit = new SplitHitChecker(num, false);
                    result.Add(horizontalHit);
                }
                if (DeniedStreets.Contains(street) == false)
                {
                    var verticalHit = new SplitHitChecker(num, true);
                    result.Add(verticalHit);
                }
                
            }

            return result;
        }

        public override string ToString()
        {
            return $"{this.GetType().Name} ( {this.HitNumber1}, {this.HitNumber2} )";
        }
    }


}
