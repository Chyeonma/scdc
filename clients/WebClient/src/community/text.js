import { whitespaceRanges, ignorableRanges } from './textPolicy.js';

const parseRanges = (values) => values.map((value) => {
  const [start, end = start] = value.split('-').map((part) => Number.parseInt(part, 16));
  return [start, end];
});
const whitespace = parseRanges(whitespaceRanges);
const ignorable = parseRanges(ignorableRanges);
const inRanges = (value, ranges) => ranges.some(([start, end]) => value >= start && value <= end);
const control = (value) => value <= 0x1f || (value >= 0x7f && value <= 0x9f);
export const isValidUnicode = (text) => typeof text === 'string' && [...text].every((character) => {
  const value = character.codePointAt(0);
  return value !== 0 && !(value >= 0xd800 && value <= 0xdfff);
});
export function trimWhitespace(text) {
  const characters = [...text];
  let start = 0;
  let end = characters.length;
  while (start < end && inRanges(characters[start].codePointAt(0), whitespace)) start++;
  while (end > start && inRanges(characters[end - 1].codePointAt(0), whitespace)) end--;
  return characters.slice(start, end).join('');
}
export function validateServerInput(input) {
  const errors = {};
  const name = typeof input.name === 'string' ? trimWhitespace(input.name) : '';
  const description = input.description == null || input.description === ''
    ? null : typeof input.description === 'string' ? input.description.replace(/\r\n?/g, '\n') : input.description;
  const visibility = input.visibility ?? 'public';
  const scalars = [...name].map((character) => character.codePointAt(0));
  if (!isValidUnicode(name) || name.length < 2 || name.length > 100
      || scalars.every((value) => control(value) || inRanges(value, whitespace) || inRanges(value, ignorable))
      || scalars.some((value) => control(value) || value === 0x2028 || value === 0x2029)) {
    errors.name = ['Tên cần 2–100 ký tự, có nội dung và nằm trên một dòng.'];
  }
  if (description !== null && (!isValidUnicode(description) || description.length > 1000)) {
    errors.description = ['Mô tả cần văn bản hợp lệ, tối đa 1.000 ký tự.'];
  }
  if (!['public', 'private'].includes(visibility)) errors.visibility = ['Chọn công khai hoặc riêng tư.'];
  return { data: { name, description, visibility }, errors };
}
