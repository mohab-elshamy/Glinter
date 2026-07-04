// @vitest-environment jsdom

import "@testing-library/jest-dom/vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { TagInput } from "./tag-input";
import { normalizeTags } from "./tag-utils";

afterEach(cleanup);

describe("TagInput", () => {
  it("normalizes, deduplicates, and limits tags", () => {
    expect(normalizeTags(
      [" Arabic ", "arabic", "", "English", "French"],
      2,
    )).toEqual(["Arabic", "English"]);
  });

  it("adds with Enter and removes with the keyboard-accessible button", () => {
    const onChange = vi.fn();
    const { rerender } = render(
      <TagInput aria-label="Languages" value={[]} onChange={onChange} />,
    );
    const input = screen.getByLabelText("Languages");
    fireEvent.change(input, { target: { value: "Arabic" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onChange).toHaveBeenCalledWith(["Arabic"]);

    rerender(
      <TagInput aria-label="Languages" value={["Arabic"]} onChange={onChange} />,
    );
    fireEvent.click(screen.getByRole("button", { name: "Remove Arabic" }));
    expect(onChange).toHaveBeenLastCalledWith([]);
  });

  it("uses readable token classes and supports disabled state", () => {
    render(
      <TagInput
        aria-label="Languages"
        value={[]}
        onChange={() => undefined}
        disabled
        placeholder="Type a language"
      />,
    );
    const input = screen.getByLabelText("Languages");
    expect(input).toBeDisabled();
    expect(input).toHaveClass("bg-background", "text-foreground");
  });
});
