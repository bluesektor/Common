// Copyright (c) 2017 GreenWerx.org.
//Licensed under CPAL 1.0,  See license.txt  or go to http://greenwerx.org/docs/license.txt  for full license details.
using GreenWerx.Models.App;
using GreenWerx.Models.Datasets;

namespace GreenWerx.Models
{
    public interface ICrud
    {
        ServiceResult Delete(INode n, bool purge = false);

        ServiceResult Get(string uuid );

        ServiceResult Insert(INode n);

        ServiceResult Update(INode n);
    }
}