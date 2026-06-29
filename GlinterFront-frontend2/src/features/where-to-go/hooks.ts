import { useState, useCallback, useEffect } from "react";
import { useSearchParams } from "react-router-dom";
import type { DayPlan } from "./data";
import { allActivities, transportOptions, vibeReasons } from "./data";
import { vibeOptions } from "./data";

export function useWhereToGo() {
  const [searchParams] = useSearchParams();
  const [selectedVibes, setSelectedVibes] = useState<string[]>([]);
  const [numDays, setNumDays] = useState(1);
  const [startTime, setStartTime] = useState("09:00");
  const [endTime, setEndTime] = useState("21:00");
  const [budget, setBudget] = useState("$$ Moderate");
  const [generatedPlan, setGeneratedPlan] = useState<DayPlan[]>([]);
  const [isGenerating, setIsGenerating] = useState(false);
  const [hasGenerated, setHasGenerated] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");

  useEffect(() => {
    const vibe = searchParams.get("vibe");
    if (vibe && vibeOptions.includes(vibe)) {
      setSelectedVibes([vibe]);
      setHasGenerated(false);
    }
  }, [searchParams]);

  const toggleVibe = (v: string) => {
    setSelectedVibes((prev) =>
      prev.includes(v) ? prev.filter((x) => x !== v) : [...prev, v]
    );
  };

  const generateItinerary = useCallback(() => {
    if (selectedVibes.length === 0) return;

    setIsGenerating(true);
    setGeneratedPlan([]);

    setTimeout(() => {
      const days: DayPlan[] = [];
      const costMultiplier = budget === "$ Budget" ? 0.6 : budget === "$$$ Luxury" ? 2.0 : 1.0;

      for (let d = 0; d < Math.min(numDays, 5); d++) {
        const vibeIndex = d % selectedVibes.length;
        const vibe = selectedVibes[vibeIndex];
        const activities = allActivities[vibe] || allActivities["Culture & History"];

        const startHour = parseInt(startTime.split(":")[0]);
        const endHour = parseInt(endTime.split(":")[0]);
        const hoursAvailable = endHour - startHour;
        const maxItems = Math.min(activities.length, Math.max(2, Math.floor(hoursAvailable / 2)));
        const dayItems = activities.slice(0, maxItems);

        const baseCost = Math.round((350 + Math.random() * 500) * costMultiplier);

        days.push({
          day: `Day ${d + 1} — ${vibe}`,
          items: dayItems,
          transport: transportOptions[d % transportOptions.length],
          cost: `EGP ${baseCost}`,
          why: vibeReasons[vibe] || "Personalized based on your preferences.",
        });
      }

      setGeneratedPlan(days);
      setIsGenerating(false);
      setHasGenerated(true);
    }, 1500);
  }, [selectedVibes, numDays, startTime, endTime, budget]);

  const removeStop = (dayIndex: number, itemIndex: number) => {
    setGeneratedPlan((prev) =>
      prev.map((day, di) =>
        di === dayIndex ? { ...day, items: day.items.filter((_, ii) => ii !== itemIndex) } : day
      )
    );
  };

  const totalCost = generatedPlan.reduce(
    (sum, d) => sum + parseInt(d.cost.replace(/[^0-9]/g, "")), 0
  );

  return {
    selectedVibes, toggleVibe,
    numDays, setNumDays,
    startTime, setStartTime,
    endTime, setEndTime,
    budget, setBudget,
    generatedPlan,
    isGenerating,
    hasGenerated,
    searchQuery, setSearchQuery,
    generateItinerary,
    removeStop,
    totalCost,
  };
}
