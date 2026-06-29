import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { Loader2, UserRoundCheck } from "lucide-react";
import Navbar from "@/components/Navbar";
import Footer from "@/components/Footer";
import { authStorage, type AppRole } from "@/shared/lib/auth";
import { getRoleHome } from "@/shared/lib/auth-routing";
import { myProfileQueryKey } from "@/shared/hooks/use-my-profile";
import { profilesApi, type MyProfileResponse } from "@/shared/services/api-profiles";

const inputClass =
  "mt-1 w-full rounded-lg border border-border bg-secondary px-3 py-2.5 text-sm " +
  "text-foreground focus:outline-none focus:ring-1 focus:ring-primary";

const getPrimaryRole = (roles: readonly string[]): AppRole =>
  (["Traveler", "LocalBuddy", "HotelOwner", "ExperienceProvider"] as const)
    .find((role) => roles.includes(role)) ?? "Traveler";

const ProfileSetupPage = () => {
  const user = authStorage.getUser()!;
  const role = getPrimaryRole(user.roles);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const [displayName, setDisplayName] = useState(user.fullName);
  const [bio, setBio] = useState("");
  const [nationality, setNationality] = useState("");
  const [budget, setBudget] = useState("Mid-range");
  const [travelStyle, setTravelStyle] = useState("Solo");
  const [preferredInterests, setPreferredInterests] = useState("");
  const [city, setCity] = useState("");
  const [languages, setLanguages] = useState("");
  const [businessName, setBusinessName] = useState(user.fullName);
  const [contactPersonName, setContactPersonName] = useState(user.fullName);
  const [phoneNumber, setPhoneNumber] = useState("");
  const [description, setDescription] = useState("");

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError("");
    setLoading(true);

    try {
      let profile: MyProfileResponse;
      if (role === "Traveler") {
        profile = await profilesApi.updateTravelerProfile({
          displayName,
          bio,
          nationality,
          preferredBudgetLevel: budget,
          travelStyle,
          preferredInterests,
          interestIds: [],
        });
      } else if (role === "LocalBuddy") {
        profile = await profilesApi.updateLocalBuddyProfile({
          displayName,
          bio,
          city,
          languages,
          interestIds: [],
        });
      } else {
        const payload = {
          businessName,
          contactPersonName,
          phoneNumber,
          description,
        };
        profile = role === "HotelOwner"
          ? await profilesApi.updateHotelOwnerProfile(payload)
          : await profilesApi.updateExperienceProviderProfile(payload);
      }

      queryClient.setQueryData(myProfileQueryKey(user.userId), profile);
      navigate(getRoleHome(user.roles), { replace: true });
    } catch (requestError) {
      setError(requestError instanceof Error
        ? requestError.message
        : "Profile setup failed.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-background">
      <Navbar solid />
      <main className="mx-auto max-w-2xl px-4 py-10">
        <div className="card-glass p-6 sm:p-8">
          <UserRoundCheck className="mx-auto mb-3 h-10 w-10 text-primary" />
          <h1 className="text-center text-2xl font-bold">Complete your profile</h1>
          <p className="mb-6 mt-2 text-center text-sm text-muted-foreground">
            Add the information required for your {role.replace(/([A-Z])/g, " $1").trim()} account.
          </p>

          {error && (
            <div role="alert" className="mb-4 rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
              {error}
            </div>
          )}

          <form className="space-y-4" onSubmit={handleSubmit}>
            {(role === "Traveler" || role === "LocalBuddy") && (
              <>
                <label className="block text-sm font-medium">
                  Display name
                  <input required value={displayName} onChange={(event) => setDisplayName(event.target.value)} className={inputClass} />
                </label>
                <label className="block text-sm font-medium">
                  Bio
                  <textarea value={bio} onChange={(event) => setBio(event.target.value)} rows={3} className={inputClass} />
                </label>
              </>
            )}

            {role === "Traveler" && (
              <>
                <label className="block text-sm font-medium">
                  Nationality
                  <input value={nationality} onChange={(event) => setNationality(event.target.value)} className={inputClass} />
                </label>
                <label className="block text-sm font-medium">
                  Preferred budget
                  <select value={budget} onChange={(event) => setBudget(event.target.value)} className={inputClass}>
                    <option>Budget</option>
                    <option>Mid-range</option>
                    <option>Luxury</option>
                  </select>
                </label>
                <label className="block text-sm font-medium">
                  Travel style
                  <input value={travelStyle} onChange={(event) => setTravelStyle(event.target.value)} className={inputClass} />
                </label>
                <label className="block text-sm font-medium">
                  Preferred interests
                  <input value={preferredInterests} onChange={(event) => setPreferredInterests(event.target.value)} placeholder="Culture, food, adventure..." className={inputClass} />
                </label>
              </>
            )}

            {role === "LocalBuddy" && (
              <>
                <label className="block text-sm font-medium">
                  City
                  <input required value={city} onChange={(event) => setCity(event.target.value)} className={inputClass} />
                </label>
                <label className="block text-sm font-medium">
                  Languages
                  <input value={languages} onChange={(event) => setLanguages(event.target.value)} placeholder="Arabic, English" className={inputClass} />
                </label>
              </>
            )}

            {(role === "HotelOwner" || role === "ExperienceProvider") && (
              <>
                <label className="block text-sm font-medium">
                  Business name
                  <input required value={businessName} onChange={(event) => setBusinessName(event.target.value)} className={inputClass} />
                </label>
                <label className="block text-sm font-medium">
                  Contact person
                  <input value={contactPersonName} onChange={(event) => setContactPersonName(event.target.value)} className={inputClass} />
                </label>
                <label className="block text-sm font-medium">
                  Phone number
                  <input value={phoneNumber} onChange={(event) => setPhoneNumber(event.target.value)} className={inputClass} />
                </label>
                <label className="block text-sm font-medium">
                  Description
                  <textarea value={description} onChange={(event) => setDescription(event.target.value)} rows={3} className={inputClass} />
                </label>
              </>
            )}

            <button type="submit" disabled={loading} className="flex w-full items-center justify-center gap-2 rounded-lg bg-primary py-3 font-semibold text-primary-foreground disabled:opacity-50">
              {loading && <Loader2 className="h-4 w-4 animate-spin" />}
              {loading ? "Saving..." : "Save and continue"}
            </button>
          </form>
        </div>
      </main>
      <Footer />
    </div>
  );
};

export default ProfileSetupPage;
