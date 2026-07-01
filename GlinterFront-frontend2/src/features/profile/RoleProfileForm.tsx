import { useState } from "react";
import { Check, Loader2, Save } from "lucide-react";
import type { ProfileRole } from "@/shared/lib/auth-routing";
import { useInterests } from "@/shared/hooks/use-interests";
import {
  profilesApi,
  type MyProfileResponse,
} from "@/shared/services/api-profiles";

interface RoleProfileFormProps {
  role: ProfileRole;
  defaultName: string;
  initialProfile?: MyProfileResponse;
  submitLabel: string;
  onSaved: (profile: MyProfileResponse) => void;
}

const inputClass =
  "mt-1 w-full rounded-lg border border-border bg-secondary px-3 py-2.5 text-sm " +
  "text-foreground focus:outline-none focus:ring-1 focus:ring-primary";

const RoleProfileForm = ({
  role,
  defaultName,
  initialProfile,
  submitLabel,
  onSaved,
}: RoleProfileFormProps) => {
  const traveler = initialProfile?.profileType === "Traveler"
    ? initialProfile
    : undefined;
  const buddy = initialProfile?.profileType === "LocalBuddy"
    ? initialProfile
    : undefined;
  const business = initialProfile?.profileType === "HotelOwner" ||
    initialProfile?.profileType === "ExperienceProvider"
    ? initialProfile
    : undefined;
  const supportsInterests = role === "Traveler" || role === "LocalBuddy";
  const interestsQuery = useInterests(supportsInterests);

  const [displayName, setDisplayName] = useState(
    traveler?.displayName ?? buddy?.displayName ?? defaultName,
  );
  const [bio, setBio] = useState(traveler?.bio ?? buddy?.bio ?? "");
  const [nationality, setNationality] = useState(traveler?.nationality ?? "");
  const [budget, setBudget] = useState(
    traveler?.preferredBudgetLevel ?? "Mid-range",
  );
  const [travelStyle, setTravelStyle] = useState(
    traveler?.travelStyle ?? "Solo",
  );
  const [city, setCity] = useState(buddy?.city ?? "");
  const [languages, setLanguages] = useState(buddy?.languages ?? "");
  const [businessName, setBusinessName] = useState(
    business?.businessName ?? defaultName,
  );
  const [contactPersonName, setContactPersonName] = useState(
    business?.contactPersonName ?? defaultName,
  );
  const [phoneNumber, setPhoneNumber] = useState(business?.phoneNumber ?? "");
  const [description, setDescription] = useState(business?.description ?? "");
  const [profileImageUrl, setProfileImageUrl] = useState(
    initialProfile?.profileImageUrl ?? "",
  );
  const [selectedInterestIds, setSelectedInterestIds] = useState<string[]>(
    traveler?.interests.map((interest) => interest.id) ??
    buddy?.interests.map((interest) => interest.id) ??
    [],
  );
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const toggleInterest = (interestId: string) => {
    setSelectedInterestIds((current) => current.includes(interestId)
      ? current.filter((id) => id !== interestId)
      : [...current, interestId]);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError("");
    setSaving(true);

    try {
      let savedProfile: MyProfileResponse;
      if (role === "Traveler") {
        const selectedNames = (interestsQuery.data ?? [])
          .filter((interest) => selectedInterestIds.includes(interest.id))
          .map((interest) => interest.name);
        savedProfile = await profilesApi.updateTravelerProfile({
          displayName: displayName.trim(),
          bio: bio.trim() || undefined,
          nationality: nationality.trim() || undefined,
          preferredBudgetLevel: budget,
          travelStyle,
          preferredInterests: selectedNames.join(", ") || undefined,
          interestIds: selectedInterestIds,
        });
      } else if (role === "LocalBuddy") {
        savedProfile = await profilesApi.updateLocalBuddyProfile({
          displayName: displayName.trim(),
          bio: bio.trim() || undefined,
          city: city.trim(),
          languages: languages.trim() || undefined,
          interestIds: selectedInterestIds,
        });
      } else {
        const payload = {
          businessName: businessName.trim(),
          contactPersonName: contactPersonName.trim() || undefined,
          phoneNumber: phoneNumber.trim() || undefined,
          description: description.trim() || undefined,
        };
        savedProfile = role === "HotelOwner"
          ? await profilesApi.updateHotelOwnerProfile(payload)
          : await profilesApi.updateExperienceProviderProfile(payload);
      }

      const imageUrl = profileImageUrl.trim();
      if (imageUrl && imageUrl !== (initialProfile?.profileImageUrl ?? "")) {
        savedProfile = await profilesApi.updateProfileImage(imageUrl);
      }

      onSaved(savedProfile);
    } catch (requestError) {
      setError(requestError instanceof Error
        ? requestError.message
        : "Profile could not be saved.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <form className="space-y-4" onSubmit={handleSubmit}>
      {error && (
        <div role="alert" className="rounded-lg border border-destructive/30 bg-destructive/10 p-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {(role === "Traveler" || role === "LocalBuddy") && (
        <>
          <label className="block text-sm font-medium">
            Display name
            <input
              required
              maxLength={200}
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              className={inputClass}
            />
          </label>
          <label className="block text-sm font-medium">
            Bio
            <textarea
              maxLength={1000}
              value={bio}
              onChange={(event) => setBio(event.target.value)}
              rows={3}
              className={inputClass}
            />
          </label>
        </>
      )}

      {role === "Traveler" && (
        <>
          <label className="block text-sm font-medium">
            Nationality
            <input
              maxLength={100}
              value={nationality}
              onChange={(event) => setNationality(event.target.value)}
              className={inputClass}
            />
          </label>
          <label className="block text-sm font-medium">
            Preferred budget
            <select
              value={budget}
              onChange={(event) => setBudget(event.target.value)}
              className={inputClass}
            >
              <option>Budget</option>
              <option>Mid-range</option>
              <option>Luxury</option>
            </select>
          </label>
          <label className="block text-sm font-medium">
            Travel style
            <select
              value={travelStyle}
              onChange={(event) => setTravelStyle(event.target.value)}
              className={inputClass}
            >
              <option>Solo</option>
              <option>Couple</option>
              <option>Family</option>
              <option>Group</option>
              <option>Business</option>
            </select>
          </label>
        </>
      )}

      {role === "LocalBuddy" && (
        <>
          <label className="block text-sm font-medium">
            City
            <input
              required
              maxLength={100}
              value={city}
              onChange={(event) => setCity(event.target.value)}
              className={inputClass}
            />
          </label>
          <label className="block text-sm font-medium">
            Languages
            <input
              maxLength={500}
              value={languages}
              onChange={(event) => setLanguages(event.target.value)}
              placeholder="Arabic, English"
              className={inputClass}
            />
          </label>
        </>
      )}

      {(role === "HotelOwner" || role === "ExperienceProvider") && (
        <>
          <label className="block text-sm font-medium">
            Business name
            <input
              required
              maxLength={200}
              value={businessName}
              onChange={(event) => setBusinessName(event.target.value)}
              className={inputClass}
            />
          </label>
          <label className="block text-sm font-medium">
            Contact person
            <input
              maxLength={200}
              value={contactPersonName}
              onChange={(event) => setContactPersonName(event.target.value)}
              className={inputClass}
            />
          </label>
          <label className="block text-sm font-medium">
            Phone number
            <input
              maxLength={50}
              value={phoneNumber}
              onChange={(event) => setPhoneNumber(event.target.value)}
              className={inputClass}
            />
          </label>
          <label className="block text-sm font-medium">
            Description
            <textarea
              maxLength={1000}
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              rows={3}
              className={inputClass}
            />
          </label>
        </>
      )}

      {supportsInterests && (
        <fieldset>
          <legend className="mb-2 text-sm font-medium">Interests</legend>
          {interestsQuery.isPending && (
            <p className="text-sm text-muted-foreground">Loading interests...</p>
          )}
          {interestsQuery.isError && (
            <button
              type="button"
              onClick={() => interestsQuery.refetch()}
              className="text-sm text-destructive hover:underline"
            >
              Interests could not be loaded. Try again.
            </button>
          )}
          <div className="flex flex-wrap gap-2">
            {(interestsQuery.data ?? []).map((interest) => {
              const selected = selectedInterestIds.includes(interest.id);
              return (
                <button
                  key={interest.id}
                  type="button"
                  aria-pressed={selected}
                  onClick={() => toggleInterest(interest.id)}
                  className={`flex items-center gap-1 rounded-full px-3 py-1.5 text-xs transition-all ${
                    selected
                      ? "bg-accent text-accent-foreground"
                      : "bg-secondary text-muted-foreground hover:bg-accent/20"
                  }`}
                >
                  {interest.name}
                  {selected && <Check className="h-3 w-3" />}
                </button>
              );
            })}
          </div>
        </fieldset>
      )}

      <label className="block text-sm font-medium">
        Profile image URL
        <input
          type="url"
          maxLength={1000}
          value={profileImageUrl}
          onChange={(event) => setProfileImageUrl(event.target.value)}
          placeholder="https://..."
          className={inputClass}
        />
      </label>

      <button
        type="submit"
        disabled={saving || (supportsInterests && interestsQuery.isPending)}
        className="flex w-full items-center justify-center gap-2 rounded-lg bg-primary py-3 font-semibold text-primary-foreground disabled:opacity-50"
      >
        {saving
          ? <Loader2 className="h-4 w-4 animate-spin" />
          : <Save className="h-4 w-4" />}
        {saving ? "Saving..." : submitLabel}
      </button>
    </form>
  );
};

export default RoleProfileForm;
