#!/usr/bin/env python3

# this script keeps the tracklandmarksdata.json file formatted in a consistent manner,
# and removes entries with default values.

import json
import os

with open("trackLandmarksData.json") as dbin:
    data = json.load(dbin)
    tracks = data["TrackLandmarksData"]
    for track in tracks:
        for field in track.copy():
            if track[field] == -1 or track[field] is None or track[field] is False or track[field] == "":
                del track[field]

        if "trackLandmarks" not in track:
            track["trackLandmarks"] = []

        landmarks = track["trackLandmarks"]
        for landmark in landmarks:
            for field in landmark.copy():
                if landmark[field] == -1 or landmark[field] is None or landmark[field] is False or landmark[field] == "":
                    del landmark[field]

    json.dump(data, open("trackLandmarksData.json", "w"), sort_keys=False, indent="\t")
