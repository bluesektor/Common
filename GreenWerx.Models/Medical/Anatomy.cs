﻿// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
namespace GreenWerx.Models.Medical
{
    public class Anatomy : Node, INode
    {
        public Anatomy()
        {
            this.UUIDType = "Anatomy";
        }

        //     head, neck , shoulder, arm, finger, chest,back, abdomen, leg, foot, toe

        //these locations should propbaly go in symptom/symptom log
        //general(all over),  left,right,center,front,back,upper, lower,inner,outer
        //                                top/bottom
        //   anatomy tags:
    }
}