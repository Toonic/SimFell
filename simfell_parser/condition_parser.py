"""Module for parsing SimFell condition lines."""

import typing
from typing import Any, List
import operator
import re
from rich import print  # pylint: disable=redefined-builtin

from simfell_parser.model import Condition
from simfell_parser.utils import CharacterTypeT

if typing.TYPE_CHECKING:
    from sim import Simulation


class SimFileConditionParser:
    """Class for parsing SimFell condition lines."""

    possible_operators = {
        "==": operator.eq,
        "!=": operator.ne,
        ">": operator.gt,
        ">=": operator.ge,
        "<": operator.lt,
        "<=": operator.le,
        "+": operator.add,
        "-": operator.sub,
        "*": operator.mul,
        "/": operator.truediv,
        "and": lambda x, y: x and y,
        "or": lambda x, y: x or y,
        "not": lambda x: not x,
        "xor": lambda x, y: bool(x) ^ bool(y),
    }

    def __init__(self, condition: str):
        self._condition = condition

    def convert(self, value: str) -> Any:
        """Convert a value to a Python object."""

        if value.isdigit():
            return int(value)

        if value.lower() == "true":
            return True

        if value.lower() == "false":
            return False

        return value

    def parse_expression(self, expression: str) -> Any:
        """Parse and evaluate a complex expression."""

        # Tokenize the expression
        tokens = re.split(r"(\s+|\b|\(|\))", expression)
        stack = []

        for token in tokens:
            token = token.strip()
            if not token:
                continue

            if token.isdigit():
                stack.append(int(token))
            elif token in SimFileConditionParser.possible_operators:
                if token == "not":
                    operand = stack.pop()
                    result = SimFileConditionParser.possible_operators[token](
                        operand
                    )
                else:
                    right = stack.pop()
                    left = stack.pop()
                    result = SimFileConditionParser.possible_operators[token](
                        left, right
                    )
                stack.append(result)
            elif token.lower() in ["true", "false"]:
                stack.append(token.lower() == "true")
            elif token == "(":
                stack.append(token)
            elif token == ")":
                # Evaluate the expression within parentheses
                sub_expr = []
                while stack and stack[-1] != "(":
                    sub_expr.append(stack.pop())
                stack.pop()  # Remove the '(' from the stack
                sub_expr.reverse()
                # Evaluate the sub-expression
                result = self.parse_expression(" ".join(map(str, sub_expr)))
                stack.append(result)
            else:
                # Handle variables or attributes
                stack.append(self.convert(token))

        return stack[0] if stack else None

    def parse(self) -> Condition:
        """Parse the condition."""
        for op_key, _ in SimFileConditionParser.possible_operators.items():
            if op_key in self._condition:
                left, right = self._condition.split(op_key, 1)
                return Condition(
                    left=left.strip(),
                    operator=op_key,
                    right=right.strip(),
                )

        raise ValueError(f"Invalid condition: {self._condition}")

    @staticmethod
    def evaluate_conditions(
        conditions: List[Condition], simulation: "Simulation"
    ) -> bool:
        """Checks all conditions."""
        checks: List[bool] = []

        for condition in conditions:
            # Get both values.
            left_value = SimFileConditionParser.parse_side(
                condition.left, simulation
            )
            right_value = SimFileConditionParser.parse_side(
                condition.right, simulation
            )

            # Get the Op function.
            op_func = SimFileConditionParser.possible_operators.get(
                condition.operator
            )
            # if left_value is not None or right_value is not None:
            if simulation.detailed_debug:
                print(
                    f"\t-> Checking if {left_value}"
                    + f" {condition.operator} {right_value}"
                )
            # If everything is set. We evaluate to see if condition is met.
            if (
                op_func is not None
                and left_value is not None
                and right_value is not None
            ):
                checks.append(
                    op_func(left_value, right_value),
                )

        return all(checks)

    @staticmethod
    def parse_side(condition: str, simulation: "Simulation"):
        """Parses out the side and fetches the appropriate value."""

        # If the values are floats or ints, just return as is.
        if bool(re.fullmatch(r"-?\d+(\.\d+)?", condition)):
            return float(condition)

        if condition.startswith("active_enemies"):
            return simulation.enemy_count

        if condition.startswith("character."):
            return SimFileConditionParser.get_character_value(
                condition, simulation
            )
        if condition.startswith("spell."):
            return SimFileConditionParser.get_spell_value(
                condition, simulation
            )
        if condition.startswith("buff."):
            return SimFileConditionParser.get_buff_value(condition, simulation)
        if condition.startswith("debuff."):
            return SimFileConditionParser.get_debuff_value(
                condition, simulation
            )

    @staticmethod
    def get_character_value(condition: str, simulation: "Simulation"):
        """Returns the condition value from the Character."""
        attribute_name = condition.split(".", 1)[1]
        character_value = getattr(simulation.character, attribute_name, None)
        if callable(character_value):
            character_value = character_value()
        return character_value

    @staticmethod
    def get_spell_value(condition: str, simulation: "Simulation"):
        """Returns the condition value from the Spell."""
        spell_name = condition.split(".", 2)[1]
        attribute_name = condition.split(".", 2)[2]
        spell_value = getattr(
            simulation.character.spells[spell_name], attribute_name, None
        )
        if callable(spell_value):
            spell_value = spell_value()
        return spell_value

    @staticmethod
    def get_buff_value(condition: str, simulation: "Simulation"):
        """Returns the condition value from buffs"""
        buff_name = condition.split(".", 2)[1]
        attribute_name = condition.split(".", 2)[2]
        if buff_name in simulation.character.buffs:
            buff_value = getattr(
                simulation.character.buffs[buff_name], attribute_name, None
            )
            if callable(buff_value):
                buff_value = buff_value()
            return buff_value
        return None

    @staticmethod
    def get_debuff_value(condition: str, simulation: "Simulation"):
        """Returns the condition value from debuffs"""
        debuff_name = condition.split(".", 2)[1]
        attribute_name = condition.split(".", 2)[2]
        if debuff_name in simulation.debuffs:
            debuff_value = getattr(
                simulation.debuffs[debuff_name], attribute_name, None
            )
            if callable(debuff_value):
                debuff_value = debuff_value()
            return debuff_value
        return None
