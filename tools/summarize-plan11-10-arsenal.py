"""Summarize measured outcomes without ranking losses as fast victories."""
import json
import sys
from collections import defaultdict


def summarize(path):
    data = json.load(open(path, encoding="utf-8-sig"))
    groups = defaultdict(lambda: dict(wins=0, runs=0, caps=0, deaths=0, winning_clock=0.0))
    for row in data["rows"]:
        g = groups[(row["candidate"], row["variant"])]
        g["wins"] += row["wins"]
        g["runs"] += row["seeds"]
        g["caps"] += row["caps"]
        g["deaths"] += row["candidateDeaths"]
        g["winning_clock"] += (row["meanWinningClock"] or 0) * row["wins"]
    rows = [dict(candidate=c, variant=v, **g, meanWinningClock=g["winning_clock"] / g["wins"] if g["wins"] else None) for (c, v), g in groups.items()]
    winners = []
    for candidate in sorted({r["candidate"] for r in rows}):
        relics = [r for r in rows if ".collection.relic" in r["variant"] or r["variant"].startswith("relic.")]
        relics = [r for r in relics if r["candidate"] == candidate]
        relics.sort(key=lambda r: (-r["wins"], r["meanWinningClock"] or float("inf")))
        winners.append(dict(candidate=candidate, topThree=relics[:3]))
        print(candidate, [(r["variant"], r["wins"], round(r["meanWinningClock"] or 0, 1)) for r in relics[:3]])
    return dict(source=path, runs=data["runs"], caps=sum(r["caps"] for r in rows), groups=rows, exploratoryTopThree=winners)


if __name__ == "__main__":
    result = summarize(sys.argv[1])
    if len(sys.argv) > 2:
        with open(sys.argv[2], "w", encoding="utf-8") as stream:
            json.dump(result, stream, ensure_ascii=False, indent=2)
