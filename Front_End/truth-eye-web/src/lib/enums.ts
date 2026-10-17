export const YEARS = [
  { value: 1, label: "Year 1" },
  { value: 2, label: "Year 2" },
  { value: 3, label: "Year 3" },
  { value: 4, label: "Year 4" },
  { value: 5, label: "Year 5" },
  { value: 6, label: "Year 6" },
  { value: 7, label: "Year 7" },
];

export const DEGREES = [
  { value: 1, label: "Teaching Assistant" },
  { value: 2, label: "Lecturer" },
  { value: 3, label: "Assistant Professor" },
  { value: 4, label: "Associate Professor" },
  { value: 5, label: "Professor" },
  { value: 6, label: "Other" },
];

export const labelOf = (list: { value: number; label: string }[], v: number) =>
  list.find((x) => x.value === v)?.label ?? String(v);