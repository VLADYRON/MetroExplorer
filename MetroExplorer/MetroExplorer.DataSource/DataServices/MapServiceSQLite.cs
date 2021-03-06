﻿namespace MetroExplorer.DataSource.DataServices
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using SQLite;
    using DataModels;
    using DataConfigurations;
    using System.Collections.Generic;

    public class MapServiceSQLite : IMapService
    {
        private ObservableCollection<MapModel> _dataSources;

        public MapServiceSQLite()
        {
            using (SQLiteConnection connection = new SQLiteConnection(SQLiteConfiguration.ConnectionString))
            {
                connection.CreateTable<MapModel>();
                if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Maps") == 0)
                    connection.RunInTransaction(() =>
                    {
                        connection.Insert(new MapModel(Guid.NewGuid(), "Default Map", @"ms-appx:///MetroExplorer.Components.Maps/DesignAssets/MapBackground.bmp"));
                    });

                connection.CreateTable<MapLocationModel>();
                connection.CreateTable<MapLocationFolderModel>();
            }
        }

        public async Task<ObservableCollection<MapModel>> Load()
        {
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(SQLiteConfiguration.ConnectionString);

            _dataSources = new ObservableCollection<MapModel>(await connection.QueryAsync<MapModel>("SELECT * FROM Maps"));

            return _dataSources;
        }

        public Task Add(MapModel map)
        {
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(SQLiteConfiguration.ConnectionString);
            _dataSources.Add(map);
            return connection.InsertAsync(map);
        }

        public Task Remove(MapModel map)
        {
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(SQLiteConfiguration.ConnectionString);
            _dataSources.Remove(map);
            return connection.DeleteAsync(map);
        }

        public Task Update(MapModel map)
        {
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(SQLiteConfiguration.ConnectionString);
            _dataSources.Remove(_dataSources.First(source => source.ID.Equals(map.ID)));
            _dataSources.Add(map);
            return connection.UpdateAsync(map);
        }

        private ObservableCollection<MapLocationModel> _locations;

        public async Task<ObservableCollection<MapLocationModel>> LoadLocations(Guid mapId)
        {
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(SQLiteConfiguration.ConnectionString);

            _locations = new ObservableCollection<MapLocationModel>(await connection.QueryAsync<MapLocationModel>("SELECT * FROM MapLocations WHERE MapId = ?", mapId));

            return _locations;
        }

        public async Task<int> AddLocation(MapLocationModel mapLocation, Guid mapId)
        {
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(SQLiteConfiguration.ConnectionString);

            int count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM MapLocations WHERE MapId = ? AND Latitude = ? AND Longitude = ?", mapId, mapLocation.Latitude, mapLocation.Longitude);

            if (count > 0)
                return 0;

            mapLocation.MapId = mapId;

            _locations.Add(mapLocation);

            return await connection.InsertAsync(mapLocation);
        }

        public Task RemoveLocation(MapLocationModel mapLocation)
        {
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(SQLiteConfiguration.ConnectionString);

            _locations.Remove(mapLocation);

            connection.ExecuteAsync("DELETE FROM MapLocationFolders WHERE MapLocationId = ?", mapLocation.ID);

            return connection.DeleteAsync(mapLocation);
        }

        public Task UpdateLocation(MapLocationModel mapLocation)
        {
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(SQLiteConfiguration.ConnectionString);

            _locations.Remove(_locations.First(location => location.ID == mapLocation.ID));

            _locations.Add(mapLocation);

            return connection.UpdateAsync(mapLocation);
        }

        private ObservableCollection<MapLocationFolderModel> _locationFolders;

        public async Task<ObservableCollection<MapLocationFolderModel>> LoadLocationFolders(Guid locationId)
        {
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(SQLiteConfiguration.ConnectionString);

            _locationFolders = new ObservableCollection<MapLocationFolderModel>(
                await connection.QueryAsync<MapLocationFolderModel>("SELECT * FROM MapLocationFolders WHERE MapLocationId = ?", locationId));

            return _locationFolders;
        }

        //ToDo: AddLocationFolders
        public async Task<int> AddLocationFolder(MapLocationFolderModel locationFolder)
        {
            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(SQLiteConfiguration.ConnectionString);

            _locationFolders.Add(locationFolder);

            return await connection.InsertAsync(locationFolder);
        }

        public Task RemoveLocationFolders(List<MapLocationFolderModel> locationFolders)
        {
            string query = "DELETE FROM MapLocationFolders WHERE ID IN (" +
                locationFolders.Aggregate(string.Empty, (current, next) =>
                {
                    _locationFolders.Remove(next);
                    return current + "'" + next.ID + "',";
                }).TrimEnd(',') + ")";

            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(SQLiteConfiguration.ConnectionString);

            return connection.ExecuteAsync(query);
        }

        public Task UpdateLocationFolder(MapLocationFolderModel locationFolder)
        {
            throw new NotImplementedException();
        }
    }
}
